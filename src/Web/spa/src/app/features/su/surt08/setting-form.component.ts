import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FormErrorService } from '../../../core/form-error.service';
import { ApiProblem, SystemSettingDto } from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  CheckboxComponent,
  InputComponent,
  LoadingComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';

const CREATE_SETTING_SCOPE: readonly SelectOption[] = [
  { value: 'company', label: 'ตั้งค่าประจำบริษัทที่เลือก' },
  { value: 'global', label: 'ตั้งค่าส่วนกลาง (Global)' },
];

const DATA_TYPE_OPTIONS: readonly SelectOption[] = [
  { value: 'STRING', label: 'ข้อความ (STRING)' },
  { value: 'INTEGER', label: 'จำนวนเต็ม (INTEGER)' },
  { value: 'BOOLEAN', label: 'ค่าตรรกะ true/false (BOOLEAN)' },
];

@Component({
  selector: 'app-setting-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    CheckboxComponent,
    ButtonComponent,
  ],
  templateUrl: './setting-form.component.html',
  styleUrl: './setting-form.component.scss',
})
export class SettingFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  readonly session = inject(SessionState);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);
  readonly editing = inject(ActivatedRoute).snapshot.data['editing'] === true;

  readonly loading = signal(false);
  readonly loaded = signal(!this.editing);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly setting = signal<SystemSettingDto | null>(null);
  readonly draft = signal<{ displayName: string; value: string } | null>(null);
  readonly sensitiveMode = signal(false);

  readonly scopeOptions = CREATE_SETTING_SCOPE;
  readonly dataTypeOptions = DATA_TYPE_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    scope: [this.session.companyId() ? 'company' : 'global'],
    key: [
      '',
      [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9._-]+$/)],
    ],
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    dataType: ['STRING' as 'STRING' | 'INTEGER' | 'BOOLEAN', Validators.required],
    value: ['', [Validators.maxLength(500)]],
    isSensitive: [false],
  });

  private readonly subscription = this.form.valueChanges.subscribe((value) => {
    this.sensitiveMode.set(Boolean(value.isSensitive));
    this.pending.dirty.set(this.form.dirty);
  });

  constructor() {
    if (this.editing) {
      if (!this.session.editingId('SURT08')) {
        void this.router.navigateByUrl('/su/surt08');
        return;
      }
      void this.load();
    }
  }

  async load(): Promise<void> {
    const id = this.session.editingId('SURT08');
    if (!id || this.loading()) return;
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const dto = await firstValueFrom(this.api.post<SystemSettingDto>('su/surt08/detail', { id }));
      this.setting.set(dto);
      this.sensitiveMode.set(dto.isSensitive);
      this.form.reset({
        scope: dto.companyId ? 'company' : 'global',
        key: dto.key,
        displayName: dto.displayName,
        dataType: dto.dataType,
        value: dto.isSensitive ? '' : dto.value,
        isSensitive: dto.isSensitive,
      });
      this.form.controls.key.disable();
      this.form.controls.dataType.disable();
      this.form.controls.isSensitive.disable();
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลตั้งค่าระบบไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.pending.busy() || !this.loaded() || this.loading() || this.conflict()) return;
    this.form.controls.key.setValue(this.form.controls.key.value.trim().toUpperCase());
    this.form.controls.displayName.setValue(this.form.controls.displayName.value.trim());
    this.form.controls.value.setValue(this.form.controls.value.value.trim());
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const current = this.setting();
    if (this.editing && !current) return;

    this.pending.busy.set(true);
    this.error.set('');
    try {
      const value = this.form.getRawValue();
      if (this.editing) {
        await firstValueFrom(
          this.api.put<SystemSettingDto>('su/surt08/update', {
            id: current!.id,
            displayName: value.displayName,
            value: value.value,
            version: current!.version,
          }),
        );
      } else {
        await firstValueFrom(
          this.api.post<SystemSettingDto>('su/surt08/create', {
            companyId: value.scope === 'global' ? null : this.session.companyId(),
            key: value.key,
            displayName: value.displayName,
            dataType: value.dataType,
            value: value.value,
            isSensitive: value.isSensitive,
          }),
        );
      }
      this.pending.dirty.set(false);
      this.notices.show('บันทึกค่าตั้งค่าระบบแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/su/surt08');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        const value = this.form.getRawValue();
        this.draft.set({
          displayName: value.displayName,
          value: this.sensitiveMode() ? '***MASKED***' : value.value,
        });
        this.conflict.set(true);
      } else {
        this.errors.apply(this.form, problem ?? {});
        this.error.set(problem?.detail || problem?.title || 'บันทึกไม่ได้ กรุณาตรวจข้อมูลและลองใหม่');
      }
    } finally {
      this.pending.busy.set(false);
    }
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.pending.dirty.set(false);
  }
}


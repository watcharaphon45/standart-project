import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FormErrorService } from '../../../core/form-error.service';
import {
  ApiProblem,
  DepartmentScopeOption,
  UserScopeDetail,
  UserScopeOptions,
} from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  CheckboxComponent,
  ConfirmService,
  LoadingComponent,
  RadioGroupComponent,
  RadioOption,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';

const SCOPE_RADIO_OPTIONS: readonly RadioOption[] = [
  {
    value: 'Company',
    label: 'ทั้งบริษัท (Company)',
    description: 'เข้าถึงทุกสาขา ฝ่าย และแผนกภายใต้บริษัทที่เลือก',
  },
  {
    value: 'Branch',
    label: 'รายสาขา (Branch)',
    description: 'เข้าถึงสาขาที่เลือก รวมถึงฝ่ายและแผนกภายใต้สาขานั้น',
  },
  {
    value: 'Department',
    label: 'รายแผนก (Department)',
    description: 'จำกัดสิทธิ์เฉพาะแผนกที่ระบุเท่านั้น',
  },
];

@Component({
  selector: 'app-user-scope-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    SelectComponent,
    RadioGroupComponent,
    CheckboxComponent,
    ButtonComponent,
  ],
  templateUrl: './user-scope-form.component.html',
  styleUrl: './user-scope-form.component.scss',
})
export class UserScopeFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionState);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  private readonly confirm = inject(ConfirmService);
  private readonly fb = inject(FormBuilder);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);

  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly detail = signal<UserScopeDetail | null>(null);
  readonly options = signal<UserScopeOptions>({ roles: [], branches: [], departments: [] });
  readonly draft = signal<{ roleId: string; scopeType: string; scopeId: string; isActive: boolean } | null>(
    null,
  );
  readonly selectedScopeType = signal<'Company' | 'Branch' | 'Department'>('Company');
  readonly selectedScopeId = signal('');
  readonly selectedActive = signal(true);

  readonly scopeRadioOptions = SCOPE_RADIO_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    roleId: ['', Validators.required],
    scopeType: ['Company' as 'Company' | 'Branch' | 'Department', Validators.required],
    scopeId: [''],
    isActive: [true],
  });

  readonly roleOptions = computed<readonly SelectOption[]>(() => [
    { value: '', label: '-- เลือกบทบาท --' },
    ...this.options().roles.map((role) => ({
      value: role.id,
      code: role.code,
      label: role.name,
      disabled: !role.isActive,
      disabledBadge: role.isActive ? undefined : 'ปิดใช้งาน',
    })),
  ]);

  readonly scopeTargetOptions = computed<readonly SelectOption[]>(() => {
    const scopeType = this.selectedScopeType();
    if (scopeType === 'Branch') {
      return [
        { value: '', label: '-- เลือกสาขา --' },
        ...this.options().branches.map((branch) => ({
          value: branch.id,
          code: branch.code,
          label: branch.name,
          disabled: !branch.isActive,
          disabledBadge: branch.isActive ? undefined : 'ปิดใช้งาน',
        })),
      ];
    }
    if (scopeType === 'Department') {
      return [
        { value: '', label: '-- เลือกแผนก --' },
        ...this.options().departments.map((dept) => ({
          value: dept.id,
          code: dept.code,
          label: dept.name,
          group: `${dept.branchCode} / ${dept.divisionCode}`,
          disabled: !dept.isActive,
          disabledBadge: dept.isActive ? undefined : 'ปิดใช้งาน',
        })),
      ];
    }
    return [];
  });

  readonly accessiblePreview = computed<readonly DepartmentScopeOption[]>(() => {
    if (!this.selectedActive()) return [];
    const scopeType = this.selectedScopeType();
    const scopeId = this.selectedScopeId();
    const allDepartments = this.options().departments;
    if (scopeType === 'Company') return allDepartments;
    if (scopeType === 'Branch') {
      return scopeId ? allDepartments.filter((dept) => dept.branchId === scopeId) : [];
    }
    return scopeId ? allDepartments.filter((dept) => dept.id === scopeId) : [];
  });

  private readonly subscription = this.form.valueChanges.subscribe((value) => {
    this.selectedScopeType.set((value.scopeType ?? 'Company') as 'Company' | 'Branch' | 'Department');
    this.selectedScopeId.set(value.scopeId ?? '');
    this.selectedActive.set(value.isActive ?? true);
    this.pending.dirty.set(this.form.dirty);
  });

  private readonly scopeTypeSubscription = this.form.controls.scopeType.valueChanges.subscribe(
    (nextType) => {
      if (!this.loaded()) return;
      this.onScopeTypeChanged(nextType);
    },
  );

  constructor() {
    if (!this.session.editingId('SURT04') || !this.session.companyId()) {
      void this.router.navigateByUrl('/su/surt04');
      return;
    }
    void this.load();
  }

  onScopeTypeChanged(nextType = this.form.controls.scopeType.value): void {
    this.selectedScopeType.set(nextType);
    if (nextType === 'Company') {
      this.form.controls.scopeId.setValue('');
      this.form.controls.scopeId.clearValidators();
    } else {
      this.form.controls.scopeId.setValue('');
      this.form.controls.scopeId.setValidators([Validators.required]);
    }
    this.form.controls.scopeId.updateValueAndValidity();
  }

  async load(): Promise<void> {
    if (this.loading()) return;
    const companyId = this.session.companyId();
    const userId = this.session.editingId('SURT04');
    if (!companyId || !userId) {
      void this.router.navigateByUrl('/su/surt04');
      return;
    }
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const [opts, detail] = await Promise.all([
        firstValueFrom(this.api.post<UserScopeOptions>('su/surt04/options', { companyId })),
        firstValueFrom(this.api.post<UserScopeDetail>('su/surt04/detail', { companyId, userId })),
      ]);
      this.options.set(opts);
      this.detail.set(detail);
      if (detail.scopeType === 'Company') {
        this.form.controls.scopeId.clearValidators();
      } else {
        this.form.controls.scopeId.setValidators([Validators.required]);
      }
      this.form.reset({
        roleId: detail.roleId ?? '',
        scopeType: detail.scopeType,
        scopeId: detail.scopeId ?? '',
        isActive: detail.isActive,
      });
      this.selectedScopeType.set(detail.scopeType);
      this.selectedScopeId.set(detail.scopeId ?? '');
      this.selectedActive.set(detail.isActive);
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลขอบเขตผู้ใช้งานไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.pending.busy() || !this.loaded() || this.loading() || this.conflict()) return;
    this.form.markAllAsTouched();
    const current = this.detail();
    if (this.form.invalid || !current) return;

    const value = this.form.getRawValue();
    if (current.version && current.isActive && !value.isActive) {
      const confirmed = await this.confirm.ask({
        title: 'ยืนยันการปิดใช้งานสิทธิ์ในบริษัท',
        message: `ปิดสิทธิ์ของ ${current.userName} ในบริษัทนี้และเพิกถอน Session เดิมทันทีหรือไม่?`,
        confirmText: 'ปิดใช้งานสิทธิ์',
        type: 'danger',
      });
      if (!confirmed) return;
    }

    this.pending.busy.set(true);
    this.error.set('');
    try {
      await firstValueFrom(
        this.api.put<UserScopeDetail>('su/surt04/update', {
          companyId: current.companyId,
          userId: current.userId,
          roleId: value.roleId,
          scopeType: value.scopeType,
          scopeId: value.scopeType === 'Company' ? null : value.scopeId,
          isActive: value.isActive,
          version: current.version || null,
        }),
      );
      this.pending.dirty.set(false);
      this.notices.show('บันทึกขอบเขตผู้ใช้งานแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/su/surt04');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.draft.set(value);
        this.conflict.set(true);
      } else {
        this.errors.apply(this.form, problem ?? {});
        this.error.set(
          problem?.detail || problem?.title || 'บันทึกขอบเขตไม่ได้ กรุณาตรวจข้อมูลและลองใหม่',
        );
      }
    } finally {
      this.pending.busy.set(false);
    }
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.scopeTypeSubscription.unsubscribe();
    this.pending.dirty.set(false);
  }
}

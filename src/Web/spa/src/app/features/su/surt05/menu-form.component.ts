import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FormErrorService } from '../../../core/form-error.service';
import { ApiProblem, MenuItemDto } from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { SessionState } from '../../../core/session-state.service';
import { ButtonComponent, InputComponent, LoadingComponent } from '../../../shared/ui';

@Component({
  selector: 'app-menu-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './menu-form.component.html',
  styleUrl: './menu-form.component.scss',
})
export class MenuFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly session = inject(SessionState);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);

  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly item = signal<MenuItemDto | null>(null);
  readonly draft = signal<{ displayName: string; sortOrder: number } | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    sortOrder: [1, [Validators.required, Validators.min(1), Validators.max(9999)]],
  });

  private readonly subscription = this.form.valueChanges.subscribe(() =>
    this.pending.dirty.set(this.form.dirty),
  );

  constructor() {
    if (!this.session.editingId('SURT05')) {
      void this.router.navigateByUrl('/su/surt05');
      return;
    }
    void this.load();
  }

  async load(): Promise<void> {
    const id = this.session.editingId('SURT05');
    if (!id || this.loading()) return;
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const dto = await firstValueFrom(this.api.post<MenuItemDto>('su/surt05/detail', { id }));
      this.item.set(dto);
      this.form.reset({ displayName: dto.displayName, sortOrder: dto.sortOrder });
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลเมนูไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    const current = this.item();
    if (!current || this.pending.busy() || !this.loaded() || this.loading() || this.conflict())
      return;
    this.form.controls.displayName.setValue(this.form.controls.displayName.value.trim());
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.pending.busy.set(true);
    this.error.set('');
    try {
      const value = this.form.getRawValue();
      await firstValueFrom(
        this.api.put<MenuItemDto>('su/surt05/update', {
          id: current.id,
          displayName: value.displayName,
          sortOrder: Number(value.sortOrder),
          version: current.version,
        }),
      );
      this.pending.dirty.set(false);
      this.notices.show('บันทึกการตั้งค่าเมนูแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/su/surt05');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.draft.set(this.form.getRawValue());
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


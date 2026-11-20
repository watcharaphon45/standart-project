import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FormErrorService } from '../../../core/form-error.service';
import { ApiProblem, UserAccount } from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { SessionState } from '../../../core/session-state.service';
import { ButtonComponent, ConfirmService, InputComponent, LoadingComponent } from '../../../shared/ui';

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
})
export class UserFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  private readonly confirm = inject(ConfirmService);
  private readonly fb = inject(FormBuilder);
  readonly session = inject(SessionState);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);
  readonly editing = inject(ActivatedRoute).snapshot.data['editing'] === true;

  readonly loading = signal(false);
  readonly loaded = signal(!this.editing);
  readonly error = signal('');
  readonly resetError = signal('');
  readonly resetting = signal(false);
  readonly conflict = signal(false);
  readonly draft = signal<{ userName: string; email: string } | null>(null);
  readonly userInfo = signal<UserAccount | null>(null);

  private original?: UserAccount;

  readonly form = this.fb.nonNullable.group({
    userName: [
      '',
      [Validators.required, Validators.maxLength(100), Validators.pattern(/^[A-Za-z0-9._-]+$/)],
    ],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    temporaryPassword: [
      '',
      this.editing ? [] : [Validators.required, Validators.minLength(15), Validators.maxLength(128)],
    ],
  });

  readonly resetForm = this.fb.nonNullable.group({
    temporaryPassword: [
      '',
      [Validators.required, Validators.minLength(15), Validators.maxLength(128)],
    ],
  });

  private readonly subscription = this.form.valueChanges.subscribe(() =>
    this.pending.dirty.set(this.form.dirty),
  );

  constructor() {
    if (this.editing) {
      if (!this.session.editingId('SURT01')) {
        void this.router.navigateByUrl('/su/surt01');
        return;
      }
      void this.load();
    }
  }

  async load(): Promise<void> {
    if (this.loading()) return;
    const id = this.session.editingId('SURT01');
    if (!id) {
      void this.router.navigateByUrl('/su/surt01');
      return;
    }
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const dto = await firstValueFrom(this.api.post<UserAccount>('su/surt01/detail', { id }));
      this.original = dto;
      this.userInfo.set(dto);
      this.form.reset({ userName: dto.userName, email: dto.email, temporaryPassword: '' });
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลผู้ใช้งานไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.pending.busy() || !this.loaded() || this.loading() || this.conflict()) return;
    this.form.controls.userName.setValue(this.form.controls.userName.value.trim());
    this.form.controls.email.setValue(this.form.controls.email.value.trim());
    this.form.markAllAsTouched();
    if (this.form.invalid || (this.editing && !this.original)) return;

    this.pending.busy.set(true);
    this.error.set('');
    try {
      const value = this.form.getRawValue();
      if (this.editing) {
        await firstValueFrom(
          this.api.put<UserAccount>('su/surt01/update', {
            id: this.original!.id,
            userName: value.userName,
            email: value.email,
            version: this.original!.version,
          }),
        );
      } else {
        await firstValueFrom(
          this.api.post<UserAccount>('su/surt01/create', {
            userName: value.userName,
            email: value.email,
            temporaryPassword: value.temporaryPassword,
          }),
        );
      }
      this.pending.dirty.set(false);
      this.notices.show('บันทึกข้อมูลผู้ใช้งานแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/su/surt01');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        const value = this.form.getRawValue();
        this.draft.set({ userName: value.userName, email: value.email });
        this.conflict.set(true);
      } else {
        this.errors.apply(this.form, problem ?? {});
        this.error.set(
          problem?.detail || problem?.title || 'บันทึกไม่ได้ กรุณาตรวจข้อมูลและลองใหม่',
        );
      }
    } finally {
      this.pending.busy.set(false);
    }
  }

  async resetTemporaryPassword(): Promise<void> {
    if (!this.editing || !this.original || this.resetting() || this.pending.busy()) return;
    this.resetForm.markAllAsTouched();
    if (this.resetForm.invalid) return;
    const confirmed = await this.confirm.ask({
      title: 'ยืนยันการรีเซ็ตรหัสชั่วคราว',
      message: `กำหนดรหัสชั่วคราวใหม่ให้ ${this.original.userName} และเพิกถอน Session ปัจจุบันทั้งหมดทันทีหรือไม่?`,
      confirmText: 'รีเซ็ตรหัส',
      type: 'danger',
    });
    if (!confirmed) return;

    this.resetting.set(true);
    this.resetError.set('');
    try {
      const updated = await firstValueFrom(
        this.api.post<UserAccount>('su/surt01/reset', {
          id: this.original.id,
          temporaryPassword: this.resetForm.controls.temporaryPassword.value,
          version: this.original.version,
        }),
      );
      this.original = updated;
      this.userInfo.set(updated);
      this.resetForm.reset({ temporaryPassword: '' });
      this.notices.show('รีเซ็ตรหัสชั่วคราวแล้ว (อายุ 24 ชั่วโมง)');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.conflict.set(true);
      } else {
        this.errors.apply(this.resetForm, problem ?? {});
        this.resetError.set(
          problem?.detail || problem?.title || 'รีเซ็ตรหัสผ่านไม่ได้ กรุณาลองใหม่',
        );
      }
    } finally {
      this.resetting.set(false);
    }
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.pending.dirty.set(false);
  }
}


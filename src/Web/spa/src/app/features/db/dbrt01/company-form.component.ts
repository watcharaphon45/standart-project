import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from '../../../core/api.service';
import { SessionState } from '../../../core/session-state.service';
import { AuthService } from '../../../core/auth.service';
import { FormErrorService } from '../../../core/form-error.service';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { ApiProblem, Company } from '../../../core/models';
import { ButtonComponent, InputComponent, LoadingComponent } from '../../../shared/ui';

@Component({
  selector: 'app-company-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingComponent, InputComponent, ButtonComponent],
  templateUrl: './company-form.component.html',
  styleUrl: './company-form.component.scss',
})
export class CompanyFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionState);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);
  readonly editing = inject(ActivatedRoute).snapshot.data['editing'] === true;
  readonly form = inject(FormBuilder).nonNullable.group({
    code: [
      '',
      [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)],
    ],
    name: ['', [Validators.required, Validators.maxLength(200)]],
  });
  readonly loading = signal(false);
  readonly loaded = signal(!this.editing);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly draft = signal<{ code: string; name: string } | null>(null);
  readonly locked = signal(false);
  private original?: Company;
  private readonly subscription = this.form.valueChanges.subscribe(() =>
    this.pending.dirty.set(this.form.dirty),
  );
  constructor() {
    if (this.editing) void this.load();
  }
  async load(): Promise<void> {
    if (this.loading()) return;
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const company = await firstValueFrom(
        this.api.post<Company>('db/dbrt01/detail', { id: this.session.companyId() }),
      );
      this.original = company;
      this.locked.set(company.codeLocked);
      this.form.reset({ code: company.code, name: company.name });
      if (company.codeLocked) this.form.controls.code.disable();
      else this.form.controls.code.enable();
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }
  async save(): Promise<void> {
    if (this.pending.busy() || !this.loaded() || this.loading() || this.conflict()) return;
    const value = this.form.getRawValue();
    this.form.controls.code.setValue(value.code.trim().toUpperCase());
    this.form.controls.name.setValue(value.name.trim());
    this.form.markAllAsTouched();
    if (this.form.invalid || (this.editing && !this.original)) return;
    this.pending.busy.set(true);
    this.error.set('');
    try {
      const body = {
        ...this.form.getRawValue(),
        ...(this.editing ? { id: this.original!.id, version: this.original!.version } : {}),
      };
      const company = await firstValueFrom(
        this.editing
          ? this.api.put<Company>('db/dbrt01/update', body)
          : this.api.post<Company>('db/dbrt01/create', body),
      );
      this.pending.dirty.set(false);
      await this.auth.refresh();
      this.session.selectCompany(company.id);
      this.notices.show('บันทึกข้อมูลแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/db/dbrt01');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.draft.set(this.form.getRawValue());
        this.conflict.set(true);
      } else {
        this.errors.apply(this.form, problem ?? {});
        this.error.set(problem?.detail || problem?.title || 'บันทึกไม่ได้ กรุณาตรวจผลรายการก่อนลองใหม่');
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

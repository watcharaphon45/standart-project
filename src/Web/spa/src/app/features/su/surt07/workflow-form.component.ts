import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FormErrorService } from '../../../core/form-error.service';
import {
  ApiProblem,
  ApprovalWorkflowDetailDto,
  ApprovalWorkflowOptions,
  LookupOption,
} from '../../../core/models';
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

type StepFormGroup = FormGroup<{
  stepName: FormControl<string>;
  approverRoleId: FormControl<string>;
  requireDifferentActor: FormControl<boolean>;
}>;

@Component({
  selector: 'app-workflow-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingComponent,
    InputComponent,
    SelectComponent,
    CheckboxComponent,
    ButtonComponent,
  ],
  templateUrl: './workflow-form.component.html',
  styleUrl: './workflow-form.component.scss',
})
export class WorkflowFormComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionState);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);
  readonly editing = inject(ActivatedRoute).snapshot.data['editing'] === true;

  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly locked = signal(false);
  readonly roles = signal<readonly LookupOption[]>([]);
  readonly draft = signal<{ code: string; name: string; documentType: string; stepCount: number } | null>(null);

  private original?: ApprovalWorkflowDetailDto;

  readonly roleOptions = computed<readonly SelectOption[]>(() => [
    { value: '', label: '-- เลือกบทบาทผู้อนุมัติ --' },
    ...this.roles().map((role) => ({
      value: role.id,
      code: role.code,
      label: role.name,
      disabled: !role.isActive,
      disabledBadge: role.isActive ? undefined : 'ปิดใช้งาน',
    })),
  ]);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    documentType: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    steps: this.fb.array<StepFormGroup>([this.buildStepGroup()]),
  });

  private readonly subscription = this.form.valueChanges.subscribe(() =>
    this.pending.dirty.set(this.form.dirty),
  );

  constructor() {
    if (this.editing && !this.session.editingId('SURT07')) {
      void this.router.navigateByUrl('/su/surt07');
      return;
    }
    void this.load();
  }

  get steps(): FormArray<StepFormGroup> {
    return this.form.controls.steps;
  }

  private buildStepGroup(
    stepName = '',
    approverRoleId = '',
    requireDifferentActor = true,
  ): StepFormGroup {
    return this.fb.nonNullable.group({
      stepName: [stepName, [Validators.required, Validators.maxLength(200)]],
      approverRoleId: [approverRoleId, Validators.required],
      requireDifferentActor: [requireDifferentActor],
    });
  }

  addStep(): void {
    if (this.steps.length >= 20) return;
    this.steps.push(this.buildStepGroup());
    this.steps.markAsDirty();
  }

  removeStep(index: number): void {
    if (this.steps.length <= 1) return;
    this.steps.removeAt(index);
    this.steps.markAsDirty();
  }

  async load(): Promise<void> {
    if (this.loading()) return;
    const companyId = this.session.companyId();
    if (!companyId) {
      void this.router.navigateByUrl('/su/surt07');
      return;
    }

    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      const options = await firstValueFrom(
        this.api.post<ApprovalWorkflowOptions>('su/surt07/options', { companyId }),
      );
      this.roles.set(options.roles);

      if (this.editing) {
        const id = this.session.editingId('SURT07');
        if (!id) {
          void this.router.navigateByUrl('/su/surt07');
          return;
        }
        const detail = await firstValueFrom(
          this.api.post<ApprovalWorkflowDetailDto>('su/surt07/detail', { id }),
        );
        this.original = detail;
        this.locked.set(detail.codeLocked);
        this.steps.clear();
        for (const step of detail.steps) {
          this.steps.push(
            this.buildStepGroup(step.stepName, step.approverRoleId, step.requireDifferentActor),
          );
        }
        this.form.patchValue({
          code: detail.code,
          name: detail.name,
          documentType: detail.documentType,
        });
        if (detail.codeLocked) this.form.controls.code.disable();
        else this.form.controls.code.enable();
      }

      this.form.markAsPristine();
      this.pending.dirty.set(false);
      this.conflict.set(false);
      this.loaded.set(true);
    } catch {
      this.error.set('โหลดข้อมูลขั้นตอนอนุมัติไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.pending.busy() || !this.loaded() || this.loading() || this.conflict()) return;
    this.form.controls.code.setValue(this.form.controls.code.value.trim().toUpperCase());
    this.form.controls.name.setValue(this.form.controls.name.value.trim());
    this.form.controls.documentType.setValue(
      this.form.controls.documentType.value.trim().toUpperCase(),
    );
    this.form.markAllAsTouched();
    if (this.form.invalid || (this.editing && !this.original)) return;

    this.pending.busy.set(true);
    this.error.set('');
    try {
      const value = this.form.getRawValue();
      const body = {
        ...(this.editing
          ? { id: this.original!.id, version: this.original!.version }
          : { companyId: this.session.companyId() }),
        code: value.code,
        name: value.name,
        documentType: value.documentType,
        steps: value.steps.map((step, index) => ({
          stepOrder: index + 1,
          stepName: step.stepName.trim(),
          approverRoleId: step.approverRoleId,
          requireDifferentActor: step.requireDifferentActor,
        })),
      };
      await firstValueFrom(
        this.editing
          ? this.api.put<ApprovalWorkflowDetailDto>('su/surt07/update', body)
          : this.api.post<ApprovalWorkflowDetailDto>('su/surt07/create', body),
      );
      this.pending.dirty.set(false);
      this.notices.show('บันทึกขั้นตอนอนุมัติแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl('/su/surt07');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        const value = this.form.getRawValue();
        this.draft.set({
          code: value.code,
          name: value.name,
          documentType: value.documentType,
          stepCount: value.steps.length,
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


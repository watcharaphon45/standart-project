import { Directive, OnDestroy, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SessionState } from '../../../core/session-state.service';
import { NotificationService } from '../../../core/notification.service';
import { FormErrorService } from '../../../core/form-error.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { ApiProblem, Page } from '../../../core/models';
import { ConfirmService, SelectOption } from '../../../shared/ui';

export interface OrganizationRecord {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
}

export interface OrganizationRow extends OrganizationRecord {
  [key: string]: string | boolean;
}

export const STATUS_FILTER_OPTIONS: readonly SelectOption[] = [
  { value: '', label: 'ทั้งหมด' },
  { value: 'true', label: 'เปิดใช้งาน' },
  { value: 'false', label: 'ปิดใช้งาน' },
];

@Directive()
export abstract class OrganizationListBase<TRow extends OrganizationRow> {
  readonly session = inject(SessionState);
  protected readonly api = inject(ApiService);
  protected readonly confirm = inject(ConfirmService);
  protected readonly notices = inject(NotificationService);
  protected readonly router = inject(Router);

  readonly rows = signal<TRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly saving = signal(false);
  readonly statusOptions = STATUS_FILTER_OPTIONS;

  keyword = '';
  active = '';
  page = 1;
  pageSize = 25;
  sortBy = 'code';
  sortDirection = 'asc';
  private request = 0;

  protected abstract readonly programCode: string;
  protected abstract readonly apiBase: string;
  protected abstract readonly routeBase: string;
  protected abstract readonly entityLabel: string;
  protected readonly requiresCompany: boolean = true;

  protected initAutoLoad(): void {
    effect(() => {
      const companyId = this.session.companyId();
      const userId = this.session.user()?.id ?? '';
      if (this.requiresCompany) {
        this.page = 1;
        this.onCompanyChanged(companyId);
        void this.load(companyId);
      } else if (userId) {
        this.page = 1;
        void this.load(companyId);
      }
    });
  }

  protected onCompanyChanged(_companyId: string): void {}

  protected buildSearchBody(companyId: string): Record<string, unknown> {
    return {
      ...(this.requiresCompany ? { companyId } : {}),
      keyword: this.keyword.trim() || null,
      isActive: this.active === '' ? null : this.active === 'true',
      pageNumber: this.page,
      pageSize: this.pageSize,
      sortField: this.sortBy,
      sortDirection: this.sortDirection,
    };
  }

  async load(companyId = this.session.companyId()): Promise<void> {
    const request = ++this.request;
    this.failed.set(false);
    if ((this.requiresCompany && !companyId) || !this.session.hasAction(this.programCode, 'VIEW')) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<TRow>>(`${this.apiBase}/search`, this.buildSearchBody(companyId)),
      );
      if (request === this.request) {
        this.rows.set(result.items);
        this.total.set(result.totalCount);
      }
    } catch {
      if (request === this.request) {
        this.failed.set(true);
        this.rows.set([]);
      }
    } finally {
      if (request === this.request) this.loading.set(false);
    }
  }

  get activeFilterCount(): number {
    return this.active !== '' ? 1 : 0;
  }

  resetFilters(): void {
    this.keyword = '';
    this.active = '';
    this.search();
  }

  search(): void {
    this.page = 1;
    void this.load();
  }

  sort(field: string): void {
    this.sortDirection = this.sortBy === field && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sortBy = field;
    this.search();
  }

  pageTo(page: number): void {
    this.page = page;
    void this.load();
  }

  resize(size: number): void {
    this.pageSize = size;
    this.search();
  }

  edit(id: string): void {
    this.session.setEditingId(this.programCode, id);
    void this.router.navigateByUrl(`${this.routeBase}/edit`);
  }

  statusById(id: string): void {
    const row = this.rows().find((item) => item.id === id);
    if (row) void this.toggleStatus(row);
  }

  async toggleStatus(row: TRow): Promise<void> {
    const actionWord = row.isActive ? 'ปิด' : 'เปิด';
    if (
      this.saving() ||
      !(await this.confirm.ask({
        title: `ยืนยันการ${actionWord}ใช้งาน`,
        message: `${actionWord}ใช้งาน${this.entityLabel} ${row.name} หรือไม่?`,
        confirmText: `${actionWord}ใช้งาน`,
        type: row.isActive ? 'danger' : 'primary',
      }))
    ) {
      return;
    }
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.post(`${this.apiBase}/${row.isActive ? 'deactivate' : 'activate'}`, {
          id: row.id,
          version: row.version,
        }),
      );
      await this.load();
      this.notices.show('เปลี่ยนสถานะแล้ว');
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}

@Directive()
export abstract class OrganizationFormBase<TDto extends OrganizationRecord, TDraft> implements OnDestroy {
  protected readonly api = inject(ApiService);
  protected readonly session = inject(SessionState);
  protected readonly router = inject(Router);
  protected readonly notices = inject(NotificationService);
  protected readonly fb = inject(FormBuilder);
  readonly pending = inject(PendingChanges);
  readonly errors = inject(FormErrorService);
  readonly editing = inject(ActivatedRoute).snapshot.data['editing'] === true;

  readonly loading = signal(false);
  readonly loaded = signal(!this.editing);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly draft = signal<TDraft | null>(null);
  readonly locked = signal(false);

  protected original?: TDto;
  abstract readonly form: FormGroup;
  protected abstract readonly programCode: string;
  protected abstract readonly apiBase: string;
  protected abstract readonly routeBase: string;
  private subscription?: { unsubscribe(): void };

  static createCodeControl() {
    return [
      '',
      [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)],
    ];
  }

  static createNameControl() {
    return ['', [Validators.required, Validators.maxLength(200)]];
  }

  protected initForm(): void {
    this.subscription = this.form.valueChanges.subscribe(() =>
      this.pending.dirty.set(this.form.dirty),
    );
    if (this.editing) {
      if (!this.session.editingId(this.programCode)) {
        void this.router.navigateByUrl(this.routeBase);
        return;
      }
      void this.load();
    } else {
      void this.loadOptions();
    }
  }

  protected async loadOptions(): Promise<void> {}

  protected abstract applyLoadedDto(dto: TDto): void;
  protected abstract buildSaveBody(): Record<string, unknown>;
  protected abstract captureDraft(): TDraft;

  protected trimCodeAndName(): void {
    const codeControl = this.form.get('code');
    const nameControl = this.form.get('name');
    if (codeControl) codeControl.setValue(String(codeControl.value ?? '').trim().toUpperCase());
    if (nameControl) nameControl.setValue(String(nameControl.value ?? '').trim());
  }

  async load(): Promise<void> {
    if (this.loading()) return;
    const id = this.session.editingId(this.programCode);
    if (!id) {
      void this.router.navigateByUrl(this.routeBase);
      return;
    }
    this.loaded.set(false);
    this.loading.set(true);
    this.error.set('');
    try {
      await this.loadOptions();
      const dto = await firstValueFrom(this.api.post<TDto>(`${this.apiBase}/detail`, { id }));
      this.original = dto;
      this.locked.set(dto.codeLocked);
      this.applyLoadedDto(dto);
      const codeControl = this.form.get('code');
      if (codeControl) {
        if (dto.codeLocked) codeControl.disable();
        else codeControl.enable();
      }
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
    this.trimCodeAndName();
    this.form.markAllAsTouched();
    if (this.form.invalid || (this.editing && !this.original)) return;
    this.pending.busy.set(true);
    this.error.set('');
    try {
      const body = this.buildSaveBody();
      await firstValueFrom(
        this.editing
          ? this.api.put<TDto>(`${this.apiBase}/update`, body)
          : this.api.post<TDto>(`${this.apiBase}/create`, body),
      );
      this.pending.dirty.set(false);
      this.notices.show('บันทึกข้อมูลแล้ว');
      this.pending.busy.set(false);
      await this.router.navigateByUrl(this.routeBase);
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.draft.set(this.captureDraft());
        this.conflict.set(true);
      } else {
        this.errors.apply(this.form, problem ?? {});
        this.error.set(
          problem?.detail || problem?.title || 'บันทึกไม่ได้ กรุณาตรวจผลรายการก่อนลองใหม่',
        );
      }
    } finally {
      this.pending.busy.set(false);
    }
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.pending.dirty.set(false);
  }
}


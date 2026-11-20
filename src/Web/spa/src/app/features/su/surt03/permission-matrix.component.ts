import { Component, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  ApiProblem,
  PermissionCatalog,
  ProgramPermissionGroup,
  RoleItem,
  RolePermissionsDetail,
} from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { PendingChanges } from '../../../core/pending-changes.service';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  CheckboxComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
} from '../../../shared/ui';

const PERM_SCOPE_OPTIONS: readonly SelectOption[] = [
  { value: 'company', label: 'บทบาทของบริษัทที่เลือก' },
  { value: 'global', label: 'บทบาทส่วนกลาง (Global)' },
];

@Component({
  selector: 'app-permission-matrix',
  imports: [
    FormsModule,
    PageHeaderComponent,
    SelectComponent,
    CheckboxComponent,
    ButtonComponent,
    LoadingComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './permission-matrix.component.html',
  styleUrl: './permission-matrix.component.scss',
})
export class PermissionMatrixComponent implements OnDestroy {
  readonly session = inject(SessionState);
  readonly pending = inject(PendingChanges);
  private readonly api = inject(ApiService);
  private readonly notices = inject(NotificationService);

  readonly scopeOptions = PERM_SCOPE_OPTIONS;
  readonly scopeMode = signal<'company' | 'global'>(
    this.session.companyId() ? 'company' : 'global',
  );

  readonly loading = signal(false);
  readonly loadingDetail = signal(false);
  readonly failed = signal(false);
  readonly error = signal('');
  readonly conflict = signal(false);
  readonly saving = signal(false);

  readonly roles = signal<readonly RoleItem[]>([]);
  readonly catalog = signal<readonly ProgramPermissionGroup[]>([]);
  readonly detail = signal<RolePermissionsDetail | null>(null);
  readonly selectedPermissions = signal<string[]>([]);
  readonly grantablePermissions = signal<string[]>([]);
  readonly selectedRoleId = signal('');
  readonly draftPermissions = signal<string[] | null>(null);

  private originalKey = '';
  private seq = 0;

  readonly roleOptions = computed<readonly SelectOption[]>(() => {
    const items = this.roles();
    if (items.length === 0) return [{ value: '', label: 'ไม่มีบทบาทในขอบเขตนี้' }];
    return items.map((role) => ({
      value: role.id,
      code: role.code,
      label: role.name,
      disabledBadge: role.isActive ? undefined : 'ปิดใช้งาน',
    }));
  });

  readonly selectedSet = computed(() => new Set(this.selectedPermissions()));
  readonly grantableSet = computed(() => new Set(this.grantablePermissions()));
  readonly canEdit = computed(() => {
    const current = this.detail();
    if (!current || !current.isActive) return false;
    return current.companyId === null
      ? this.session.hasGlobal('SURT03.UPDATE')
      : this.session.hasAction('SURT03', 'UPDATE');
  });

  constructor() {
    effect(() => {
      const companyId = this.session.companyId();
      const mode = this.scopeMode();
      void this.loadCatalog(mode, companyId);
    });
  }

  async onScopeChange(mode: string): Promise<void> {
    if (!(await this.pending.leave())) return;
    this.scopeMode.set(mode === 'global' ? 'global' : 'company');
  }

  async onRoleChange(roleId: string): Promise<void> {
    if (!roleId || roleId === this.selectedRoleId()) return;
    if (!(await this.pending.leave())) return;
    this.selectedRoleId.set(roleId);
    await this.loadRoleDetail(roleId);
  }

  async loadCatalog(
    mode = this.scopeMode(),
    companyId = this.session.companyId(),
  ): Promise<void> {
    const req = ++this.seq;
    this.failed.set(false);
    this.error.set('');
    this.conflict.set(false);
    this.draftPermissions.set(null);
    const isGlobal = mode === 'global';

    const allowed = isGlobal
      ? this.session.hasGlobal('SURT03.VIEW')
      : Boolean(companyId) && this.session.hasAction('SURT03', 'VIEW');

    if (!allowed) {
      this.roles.set([]);
      this.catalog.set([]);
      this.detail.set(null);
      this.selectedRoleId.set('');
      this.selectedPermissions.set([]);
      this.pending.dirty.set(false);
      return;
    }

    this.loading.set(true);
    try {
      const response = await firstValueFrom(
        this.api.post<PermissionCatalog>('su/surt03/options', {
          companyId: isGlobal ? null : companyId,
        }),
      );
      if (req !== this.seq) return;
      this.roles.set(response.roles);
      this.catalog.set(response.catalog);
      this.grantablePermissions.set(response.grantablePermissions);

      const previousId = this.selectedRoleId();
      const nextRole =
        response.roles.find((r) => r.id === previousId) ?? response.roles[0] ?? null;
      this.selectedRoleId.set(nextRole?.id ?? '');
      if (nextRole) {
        await this.loadRoleDetail(nextRole.id);
      } else {
        this.detail.set(null);
        this.selectedPermissions.set([]);
        this.pending.dirty.set(false);
      }
    } catch {
      if (req === this.seq) {
        this.failed.set(true);
      }
    } finally {
      if (req === this.seq) this.loading.set(false);
    }
  }

  async loadRoleDetail(roleId = this.selectedRoleId()): Promise<void> {
    if (!roleId) return;
    this.loadingDetail.set(true);
    this.error.set('');
    this.conflict.set(false);
    try {
      const dto = await firstValueFrom(
        this.api.post<RolePermissionsDetail>('su/surt03/detail', { roleId }),
      );
      this.detail.set(dto);
      this.grantablePermissions.set(dto.grantablePermissions);
      const sorted = [...dto.permissions].sort();
      this.originalKey = sorted.join(',');
      this.selectedPermissions.set(sorted);
      this.pending.dirty.set(false);
    } catch {
      this.error.set('โหลดรายละเอียดสิทธิ์ของบทบาทไม่ได้ กรุณาลองใหม่');
    } finally {
      this.loadingDetail.set(false);
    }
  }

  togglePermission(permission: string, checked: boolean): void {
    const current = new Set(this.selectedPermissions());
    if (checked) current.add(permission);
    else current.delete(permission);
    const next = [...current].sort();
    this.selectedPermissions.set(next);
    this.pending.dirty.set(next.join(',') !== this.originalKey);
  }

  async save(): Promise<void> {
    const current = this.detail();
    if (!current || !this.canEdit() || this.saving() || this.conflict()) return;
    this.saving.set(true);
    this.pending.busy.set(true);
    this.error.set('');
    try {
      const updated = await firstValueFrom(
        this.api.put<RolePermissionsDetail>('su/surt03/update', {
          roleId: current.roleId,
          permissions: this.selectedPermissions(),
          version: current.version,
        }),
      );
      this.detail.set(updated);
      this.grantablePermissions.set(updated.grantablePermissions);
      const sorted = [...updated.permissions].sort();
      this.originalKey = sorted.join(',');
      this.selectedPermissions.set(sorted);
      this.draftPermissions.set(null);
      this.pending.dirty.set(false);
      this.notices.show('บันทึกสิทธิ์บทบาทแล้ว');
    } catch (error) {
      const problem = (error as HttpErrorResponse).error as ApiProblem;
      if (problem?.code === 'CONCURRENCY_CONFLICT') {
        this.draftPermissions.set([...this.selectedPermissions()]);
        this.conflict.set(true);
      } else {
        this.error.set(
          problem?.detail || problem?.title || 'บันทึกสิทธิ์ไม่ได้ กรุณาตรวจสอบขอบเขตที่อนุญาต',
        );
      }
    } finally {
      this.saving.set(false);
      this.pending.busy.set(false);
    }
  }

  ngOnDestroy(): void {
    this.pending.dirty.set(false);
  }
}


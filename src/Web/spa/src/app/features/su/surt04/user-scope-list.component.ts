import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Page, UserScopeListItem } from '../../../core/models';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableRow,
  TableToolbarComponent,
} from '../../../shared/ui';
import { STATUS_FILTER_OPTIONS } from '../../db/shared/organization-crud';

interface UserScopeTableRow extends TableRow {
  userName: string;
  email: string;
  roleCode: string;
  scopeType: string;
  isActive: boolean;
}

const SCOPE_TYPE_OPTIONS: readonly SelectOption[] = [
  { value: '', label: 'ทุกขอบเขต' },
  { value: 'Company', label: 'ทั้งบริษัท' },
  { value: 'Branch', label: 'รายสาขา' },
  { value: 'Department', label: 'รายแผนก' },
];

@Component({
  selector: 'app-user-scope-list',
  imports: [
    FormsModule,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './user-scope-list.component.html',
  styleUrl: './user-scope-list.component.scss',
})
export class UserScopeListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly rows = signal<UserScopeTableRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly scopeFilterOptions = SCOPE_TYPE_OPTIONS;
  readonly statusOptions = STATUS_FILTER_OPTIONS;

  keyword = '';
  scopeType = '';
  active = '';
  page = 1;
  pageSize = 25;
  sortBy = 'userName';
  sortDirection = 'asc';
  private req = 0;

  readonly columns: TableColumn[] = [
    { key: 'userName', label: 'ชื่อผู้ใช้งาน', sortable: true },
    { key: 'email', label: 'อีเมล', sortable: true, format: 'muted' },
    { key: 'roleCode', label: 'บทบาท', sortable: true },
    { key: 'scopeType', label: 'ขอบเขตข้อมูล', sortable: true },
    { key: 'isActive', label: 'สถานะสิทธิ์', sortable: true, format: 'status' },
  ];

  constructor() {
    effect(() => {
      const companyId = this.session.companyId();
      this.page = 1;
      void this.load(companyId);
    });
  }

  async load(companyId = this.session.companyId()): Promise<void> {
    const current = ++this.req;
    this.failed.set(false);
    if (!companyId || !this.session.hasAction('SURT04', 'VIEW')) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<UserScopeListItem>>('su/surt04/search', {
          companyId,
          keyword: this.keyword.trim() || null,
          scopeType: this.scopeType || null,
          isActive: this.active === '' ? null : this.active === 'true',
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (current === this.req) {
        this.rows.set(
          result.items.map((item) => ({
            id: item.userId,
            userName: item.userName,
            email: item.email,
            roleCode: item.hasAssignment && item.roleCode
              ? `${item.roleCode} · ${item.roleName ?? ''}`
              : 'ยังไม่กำหนดบทบาท',
            scopeType: this.formatScopeLabel(item),
            isActive: item.hasAssignment && item.isActive,
          })),
        );
        this.total.set(result.totalCount);
      }
    } catch {
      if (current === this.req) {
        this.failed.set(true);
        this.rows.set([]);
      }
    } finally {
      if (current === this.req) this.loading.set(false);
    }
  }

  get activeFilterCount(): number {
    return (this.scopeType ? 1 : 0) + (this.active !== '' ? 1 : 0);
  }

  resetFilters(): void {
    this.keyword = '';
    this.scopeType = '';
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

  edit(userId: string): void {
    this.session.setEditingId('SURT04', userId);
    void this.router.navigateByUrl('/su/surt04/edit');
  }

  private formatScopeLabel(item: UserScopeListItem): string {
    if (!item.hasAssignment || !item.scopeType) return 'ยังไม่กำหนดขอบเขต';
    if (item.scopeType === 'Company') return 'ทั้งบริษัท';
    const prefix = item.scopeType === 'Branch' ? 'สาขา' : 'แผนก';
    return `${prefix}: ${item.scopeCode ?? ''} · ${item.scopeName ?? ''}`;
  }
}


import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Page, RoleItem } from '../../../core/models';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';
import { OrganizationListBase, OrganizationRow } from '../../db/shared/organization-crud';

interface RoleTableRow extends OrganizationRow {
  companyId: string;
  permissionLabel: string;
  assignedLabel: string;
}

const ROLE_SCOPE_OPTIONS: readonly SelectOption[] = [
  { value: 'company', label: 'บทบาทของบริษัทที่เลือก' },
  { value: 'global', label: 'บทบาทส่วนกลาง (Global)' },
];

@Component({
  selector: 'app-role-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss',
})
export class RoleListComponent extends OrganizationListBase<RoleTableRow> {
  protected override readonly programCode = 'SURT02';
  protected override readonly apiBase = 'su/surt02';
  protected override readonly routeBase = '/su/surt02';
  protected override readonly entityLabel = 'บทบาท';
  protected override readonly requiresCompany = false;

  readonly scopeOptions = ROLE_SCOPE_OPTIONS;
  readonly scopeMode = signal<'company' | 'global'>(
    this.session.companyId() ? 'company' : 'global',
  );
  private loadSeq = 0;

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสบทบาท', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อบทบาท', sortable: true },
    { key: 'permissionLabel', label: 'สิทธิ์ในบทบาท', format: 'muted' },
    { key: 'assignedLabel', label: 'ผู้ใช้งาน', format: 'muted' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }

  override get activeFilterCount(): number {
    return (this.active !== '' ? 1 : 0) + (this.scopeMode() === 'global' && this.session.companyId() ? 1 : 0);
  }

  override resetFilters(): void {
    this.keyword = '';
    this.active = '';
    this.scopeMode.set(this.session.companyId() ? 'company' : 'global');
    this.search();
  }

  onScopeChange(mode: string): void {
    this.scopeMode.set(mode === 'global' ? 'global' : 'company');
    this.search();
  }

  override async load(companyId = this.session.companyId()): Promise<void> {
    const seq = ++this.loadSeq;
    const isGlobal = this.scopeMode() === 'global';
    this.failed.set(false);

    const allowed = isGlobal
      ? this.session.hasGlobal('SURT02.VIEW')
      : Boolean(companyId) && this.session.hasAction('SURT02', 'VIEW');

    if (!allowed) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<RoleItem>>('su/surt02/search', {
          companyId: isGlobal ? null : companyId,
          keyword: this.keyword.trim() || null,
          isActive: this.active === '' ? null : this.active === 'true',
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (seq === this.loadSeq) {
        this.rows.set(
          result.items.map((item) => ({
            id: item.id,
            code: item.code,
            name: item.name,
            isActive: item.isActive,
            codeLocked: item.codeLocked,
            version: item.version,
            companyId: item.companyId ?? '',
            permissionLabel: `${item.permissionCount} สิทธิ์`,
            assignedLabel: `${item.assignedUserCount} คน`,
          })),
        );
        this.total.set(result.totalCount);
      }
    } catch {
      if (seq === this.loadSeq) {
        this.failed.set(true);
        this.rows.set([]);
      }
    } finally {
      if (seq === this.loadSeq) this.loading.set(false);
    }
  }
}


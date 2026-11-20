import { Component, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Page, SystemSettingDto } from '../../../core/models';
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

interface SettingTableRow extends TableRow {
  key: string;
  displayName: string;
  dataType: string;
  valueLabel: string;
}

const SETTING_SCOPE_OPTIONS: readonly SelectOption[] = [
  { value: 'company', label: 'ตั้งค่าประจำบริษัทที่เลือก' },
  { value: 'global', label: 'ตั้งค่าส่วนกลาง (Global)' },
];

@Component({
  selector: 'app-setting-list',
  imports: [
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './setting-list.component.html',
  styleUrl: './setting-list.component.scss',
})
export class SettingListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly rows = signal<SettingTableRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly scopeOptions = SETTING_SCOPE_OPTIONS;
  readonly scopeMode = signal<'company' | 'global'>(
    this.session.hasGlobal('SURT08.VIEW') && !this.session.companyId() ? 'global' : 'company',
  );

  keyword = '';
  page = 1;
  pageSize = 25;
  sortBy = 'key';
  sortDirection = 'asc';
  private req = 0;

  readonly columns: TableColumn[] = [
    { key: 'key', label: 'คีย์ตั้งค่า', sortable: true, format: 'code' },
    { key: 'displayName', label: 'ชื่อแสดงผล', sortable: true },
    { key: 'dataType', label: 'ชนิดข้อมูล', sortable: true, format: 'muted' },
    { key: 'valueLabel', label: 'ค่าปัจจุบัน' },
  ];

  constructor() {
    effect(() => {
      const companyId = this.session.companyId();
      const mode = this.scopeMode();
      this.page = 1;
      void this.load(mode, companyId);
    });
  }

  onScopeChange(mode: string): void {
    this.scopeMode.set(mode === 'global' ? 'global' : 'company');
    this.page = 1;
  }

  async load(mode = this.scopeMode(), companyId = this.session.companyId()): Promise<void> {
    const req = ++this.req;
    const isGlobal = mode === 'global';
    this.failed.set(false);
    const allowed = isGlobal
      ? this.session.hasGlobal('SURT08.VIEW')
      : Boolean(companyId) && this.session.hasAction('SURT08', 'VIEW');

    if (!allowed) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<SystemSettingDto>>('su/surt08/search', {
          companyId: isGlobal ? null : companyId,
          keyword: this.keyword.trim() || null,
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (req === this.req) {
        this.rows.set(
          result.items.map((item) => ({
            id: item.id,
            key: item.key,
            displayName: item.displayName,
            dataType: item.dataType,
            valueLabel: item.isSensitive
              ? item.hasValue
                ? '•••••••• (ซ่อนค่าลับ)'
                : '(ยังไม่ตั้งค่า)'
              : item.value || '(ว่าง)',
          })),
        );
        this.total.set(result.totalCount);
      }
    } catch {
      if (req === this.req) {
        this.failed.set(true);
        this.rows.set([]);
      }
    } finally {
      if (req === this.req) this.loading.set(false);
    }
  }

  get activeFilterCount(): number {
    return this.scopeMode() === 'global' && this.session.companyId() ? 1 : 0;
  }

  resetFilters(): void {
    this.keyword = '';
    this.scopeMode.set(
      this.session.hasGlobal('SURT08.VIEW') && !this.session.companyId() ? 'global' : 'company',
    );
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
    this.session.setEditingId('SURT08', id);
    void this.router.navigateByUrl('/su/surt08/edit');
  }
}


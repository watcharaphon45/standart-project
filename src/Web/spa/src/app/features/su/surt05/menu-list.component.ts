import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { MenuItemDto, Page } from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  ConfirmService,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableRow,
  TableToolbarComponent,
} from '../../../shared/ui';
import { STATUS_FILTER_OPTIONS } from '../../db/shared/organization-crud';

interface MenuTableRow extends TableRow {
  programCode: string;
  displayName: string;
  route: string;
  sortOrderLabel: string;
  isActive: boolean;
}

const MODULE_OPTIONS: readonly SelectOption[] = [
  { value: '', label: 'ทุกโมดูล' },
  { value: 'DB', label: 'DB ข้อมูลองค์กร' },
  { value: 'SU', label: 'SU ระบบและการตั้งค่า' },
];

@Component({
  selector: 'app-menu-list',
  imports: [
    FormsModule,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './menu-list.component.html',
  styleUrl: './menu-list.component.scss',
})
export class MenuListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly notices = inject(NotificationService);
  private readonly router = inject(Router);

  readonly items = signal<MenuItemDto[]>([]);
  readonly rows = computed<MenuTableRow[]>(() =>
    this.items().map((item) => ({
      id: item.id,
      programCode: item.programCode,
      displayName: item.displayName,
      route: item.route,
      sortOrderLabel: String(item.sortOrder),
      isActive: item.isActive,
    })),
  );
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly saving = signal(false);
  readonly moduleOptions = MODULE_OPTIONS;
  readonly statusOptions = STATUS_FILTER_OPTIONS;

  keyword = '';
  moduleCode = '';
  active = '';
  page = 1;
  pageSize = 25;
  sortBy = 'sortOrder';
  sortDirection = 'asc';
  private req = 0;

  readonly columns: TableColumn[] = [
    { key: 'sortOrderLabel', label: 'ลำดับ', format: 'number' },
    { key: 'programCode', label: 'รหัสโปรแกรม', sortable: true, format: 'code' },
    { key: 'displayName', label: 'ชื่อแสดงผล', sortable: true },
    { key: 'route', label: 'เส้นทาง', format: 'mono' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const req = ++this.req;
    this.failed.set(false);
    if (!this.session.hasGlobal('SURT05.VIEW')) {
      this.items.set([]);
      this.total.set(0);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<MenuItemDto>>('su/surt05/search', {
          keyword: this.keyword.trim() || null,
          moduleCode: this.moduleCode || null,
          isActive: this.active === '' ? null : this.active === 'true',
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (req === this.req) {
        this.items.set(result.items);
        this.total.set(result.totalCount);
      }
    } catch {
      if (req === this.req) {
        this.failed.set(true);
        this.items.set([]);
      }
    } finally {
      if (req === this.req) this.loading.set(false);
    }
  }

  get activeFilterCount(): number {
    return (this.moduleCode ? 1 : 0) + (this.active !== '' ? 1 : 0);
  }

  resetFilters(): void {
    this.keyword = '';
    this.moduleCode = '';
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
    this.session.setEditingId('SURT05', id);
    void this.router.navigateByUrl('/su/surt05/edit');
  }

  async statusById(id: string): Promise<void> {
    const item = this.items().find((x) => x.id === id);
    if (!item || this.saving()) return;
    const actionWord = item.isActive ? 'ปิด' : 'เปิด';
    const confirmed = await this.confirm.ask({
      title: `ยืนยันการ${actionWord}แสดงเมนู`,
      message: `${actionWord}การแสดงผลเมนู ${item.programCode} · ${item.displayName} ในแถบนำทางหรือไม่?`,
      confirmText: `${actionWord}ใช้งาน`,
      type: item.isActive ? 'danger' : 'primary',
    });
    if (!confirmed) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.post(`su/surt05/${item.isActive ? 'deactivate' : 'activate'}`, {
          id: item.id,
          version: item.version,
        }),
      );
      await this.load();
      this.notices.show('อัปเดตเมนูแล้ว');
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}


import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SessionState } from '../../../core/session-state.service';
import { AuthService } from '../../../core/auth.service';
import { NotificationService } from '../../../core/notification.service';
import { Company, Page } from '../../../core/models';
import {
  ButtonComponent,
  ConfirmService,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';

@Component({
  selector: 'app-company-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './company-list.component.html',
  styleUrl: './company-list.component.scss',
})
export class CompanyListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly notices = inject(NotificationService);
  readonly rows = signal<Company[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly saving = signal(false);
  keyword = '';
  active = '';
  page = 1;
  pageSize = 25;
  sortBy = 'code';
  sortDirection = 'asc';
  private request = 0;
  readonly statusOptions: readonly SelectOption[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'true', label: 'เปิดใช้งาน' },
    { value: 'false', label: 'ปิดใช้งาน' },
  ];
  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสบริษัท', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อบริษัท', sortable: true },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];
  constructor() {
    effect(() => {
      const id = this.session.companyId();
      this.page = 1;
      void this.load(id);
    });
  }
  async load(companyId = this.session.companyId()): Promise<void> {
    const request = ++this.request;
    this.failed.set(false);
    if (!companyId || !this.session.has('DBRT01.VIEW')) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const page = await firstValueFrom(
        this.api.post<Page<Company>>('db/dbrt01/search', {
          companyId,
          keyword: this.keyword.trim() || null,
          isActive: this.active === '' ? null : this.active === 'true',
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (request === this.request) {
        this.rows.set(page.items);
        this.total.set(page.totalCount);
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
  statusById(id: string): void {
    const company = this.rows().find((row) => row.id === id);
    if (company) void this.status(company);
  }
  async status(company: Company): Promise<void> {
    if (
      this.saving() ||
      !(await this.confirm.ask(
        (company.isActive ? 'ปิด' : 'เปิด') + 'ใช้งานบริษัท ' + company.name + ' หรือไม่?',
      ))
    )
      return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.post('db/dbrt01/' + (company.isActive ? 'deactivate' : 'activate'), {
          id: company.id,
          version: company.version,
        }),
      );
      await this.auth.refresh();
      await this.load();
      this.notices.show('เปลี่ยนสถานะแล้ว');
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}

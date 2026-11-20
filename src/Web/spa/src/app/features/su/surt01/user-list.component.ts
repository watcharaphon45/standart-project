import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Page, UserAccount } from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
import { SessionState } from '../../../core/session-state.service';
import {
  ButtonComponent,
  ConfirmService,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  TableColumn,
  TableRow,
  TableToolbarComponent,
} from '../../../shared/ui';
import { STATUS_FILTER_OPTIONS } from '../../db/shared/organization-crud';

interface UserTableRow extends TableRow {
  userName: string;
  email: string;
  passwordState: string;
  lockState: string;
  isActive: boolean;
  isLockedOut: boolean;
}

@Component({
  selector: 'app-user-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly notices = inject(NotificationService);
  private readonly router = inject(Router);

  readonly users = signal<UserAccount[]>([]);
  readonly rows = computed<UserTableRow[]>(() =>
    this.users().map((item) => ({
      id: item.id,
      userName: item.userName,
      email: item.email,
      passwordState: item.mustChangePassword ? 'รหัสชั่วคราว' : 'ปกติ',
      lockState: item.isLockedOut ? 'ถูกล็อก' : 'ปกติ',
      isActive: item.isActive,
      isLockedOut: item.isLockedOut,
    })),
  );
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly saving = signal(false);
  readonly statusOptions = STATUS_FILTER_OPTIONS;

  keyword = '';
  active = '';
  page = 1;
  pageSize = 25;
  sortBy = 'userName';
  sortDirection = 'asc';
  private request = 0;

  readonly columns: TableColumn[] = [
    { key: 'userName', label: 'ชื่อผู้ใช้งาน', sortable: true },
    { key: 'email', label: 'อีเมล', sortable: true, format: 'muted' },
    { key: 'passwordState', label: 'รหัสผ่าน', format: 'badge', tones: { รหัสชั่วคราว: 'warning' } },
    { key: 'lockState', label: 'สถานะล็อก', format: 'badge', tones: { ถูกล็อก: 'error' } },
    { key: 'isActive', label: 'สถานะบัญชี', sortable: true, format: 'status' },
  ];

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const request = ++this.request;
    this.failed.set(false);
    if (!this.session.hasGlobal('SURT01.VIEW')) {
      this.users.set([]);
      this.total.set(0);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<UserAccount>>('su/surt01/search', {
          keyword: this.keyword.trim() || null,
          isActive: this.active === '' ? null : this.active === 'true',
          pageNumber: this.page,
          pageSize: this.pageSize,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      if (request === this.request) {
        this.users.set(result.items);
        this.total.set(result.totalCount);
      }
    } catch {
      if (request === this.request) {
        this.failed.set(true);
        this.users.set([]);
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
    this.session.setEditingId('SURT01', id);
    void this.router.navigateByUrl('/su/surt01/edit');
  }

  async unlockById(id: string): Promise<void> {
    const user = this.users().find((item) => item.id === id);
    if (
      !user ||
      this.saving() ||
      !(await this.confirm.ask({
        title: 'ยืนยันการปลดล็อกบัญชี',
        message: `ปลดล็อกบัญชี ${user.userName} เพื่อให้เข้าสู่ระบบได้อีกครั้งหรือไม่?`,
        confirmText: 'ปลดล็อก',
        type: 'primary',
      }))
    ) {
      return;
    }
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.post('su/surt01/unlock', { id: user.id, version: user.version }),
      );
      await this.load();
      this.notices.show('ปลดล็อกบัญชีแล้ว');
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  async statusById(id: string): Promise<void> {
    const user = this.users().find((item) => item.id === id);
    if (!user) return;
    const actionWord = user.isActive ? 'ปิด' : 'เปิด';
    if (
      this.saving() ||
      !(await this.confirm.ask({
        title: `ยืนยันการ${actionWord}ใช้งานบัญชี`,
        message: user.isActive
          ? `ปิดใช้งานบัญชี ${user.userName} และเพิกถอน Session ทั้งหมดทันทีหรือไม่?`
          : `เปิดใช้งานบัญชี ${user.userName} หรือไม่?`,
        confirmText: `${actionWord}ใช้งาน`,
        type: user.isActive ? 'danger' : 'primary',
      }))
    ) {
      return;
    }
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.post(`su/surt01/${user.isActive ? 'disable' : 'activate'}`, {
          id: user.id,
          version: user.version,
        }),
      );
      await this.load();
      this.notices.show('เปลี่ยนสถานะบัญชีแล้ว');
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}


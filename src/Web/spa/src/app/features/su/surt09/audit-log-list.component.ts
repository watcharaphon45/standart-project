import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  AuditEventDetail,
  AuditEventItem,
  AuditExportResult,
  Page,
} from '../../../core/models';
import { NotificationService } from '../../../core/notification.service';
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

interface AuditTableRow extends TableRow {
  occurredAtText: string;
  programCode: string;
  action: string;
  actorUserName: string;
  result: string;
  traceId: string;
}

const AUDIT_SCOPE_OPTIONS: readonly SelectOption[] = [
  { value: 'company', label: 'ข้อมูลบริษัทที่เลือก (ตาม Scope)' },
  { value: 'global', label: 'ประวัติส่วนกลางและทั้งระบบ (Global Auditor)' },
];

const PROGRAM_FILTER_OPTIONS: readonly SelectOption[] = [
  { value: '', label: 'ทุกโปรแกรม' },
  { value: 'DBRT01', code: 'DBRT01', label: 'ข้อมูลบริษัท', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT02', code: 'DBRT02', label: 'ข้อมูลสาขา', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT03', code: 'DBRT03', label: 'ข้อมูลแผนก', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT04', code: 'DBRT04', label: 'ข้อมูลฝ่าย', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT05', code: 'DBRT05', label: 'ตำแหน่งงาน', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT06', code: 'DBRT06', label: 'ศูนย์ต้นทุน', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'DBRT07', code: 'DBRT07', label: 'หน่วยธุรกิจ', group: 'ข้อมูลองค์กร (DB)' },
  { value: 'SURT01', code: 'SURT01', label: 'ผู้ใช้งาน', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT02', code: 'SURT02', label: 'บทบาท', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT03', code: 'SURT03', label: 'สิทธิ์บทบาท', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT04', code: 'SURT04', label: 'ขอบเขตผู้ใช้งาน', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT05', code: 'SURT05', label: 'เมนูระบบ', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT06', code: 'SURT06', label: 'เลขที่เอกสาร', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT07', code: 'SURT07', label: 'ขั้นตอนอนุมัติ', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'SURT08', code: 'SURT08', label: 'ตั้งค่าระบบ', group: 'ระบบและการตั้งค่า (SU)' },
  { value: 'AUTH', code: 'AUTH', label: 'เข้าสู่ระบบและรหัสผ่าน', group: 'ความปลอดภัยระบบ' },
];

@Component({
  selector: 'app-audit-log-list',
  imports: [
    FormsModule,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.scss',
})
export class AuditLogListComponent {
  readonly session = inject(SessionState);
  private readonly api = inject(ApiService);
  private readonly notices = inject(NotificationService);

  readonly scopeOptions = AUDIT_SCOPE_OPTIONS;
  readonly programOptions = PROGRAM_FILTER_OPTIONS;
  readonly scopeMode = signal<'company' | 'global'>(
    this.session.hasGlobal('SURT09.VIEW') && !this.session.companyId() ? 'global' : 'company',
  );

  readonly rows = signal<AuditTableRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly exporting = signal(false);
  readonly selectedDetail = signal<AuditEventDetail | null>(null);
  readonly loadingDetail = signal(false);

  keyword = '';
  programCode = '';
  actionFilter = '';
  page = 1;
  pageSize = 25;
  sortBy = 'occurredAtUtc';
  sortDirection = 'desc';
  private req = 0;

  readonly columns: TableColumn[] = [
    { key: 'occurredAtText', label: 'เวลา (UTC)', format: 'mono' },
    { key: 'programCode', label: 'โปรแกรม', sortable: true, format: 'code' },
    { key: 'action', label: 'การทำงาน', sortable: true },
    { key: 'actorUserName', label: 'ผู้ทำรายการ' },
    { key: 'result', label: 'ผลลัพธ์', sortable: true, format: 'badge', tones: { SUCCESS: 'success', FAILURE: 'error' } },
    { key: 'traceId', label: 'Trace ID', format: 'trace' },
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
    this.selectedDetail.set(null);
    this.page = 1;
  }

  async load(mode = this.scopeMode(), companyId = this.session.companyId()): Promise<void> {
    const req = ++this.req;
    const isGlobal = mode === 'global';
    this.failed.set(false);
    const allowed = isGlobal
      ? this.session.hasGlobal('SURT09.VIEW')
      : Boolean(companyId) && this.session.hasAction('SURT09', 'VIEW');

    if (!allowed) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<AuditEventItem>>('su/surt09/search', {
          companyId: isGlobal ? null : companyId,
          programCode: this.programCode || null,
          action: this.actionFilter.trim() || null,
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
            occurredAtText: item.occurredAtUtc.replace('T', ' ').slice(0, 19),
            programCode: item.programCode,
            action: item.action,
            actorUserName: item.actorUserName || '-',
            result: item.result,
            traceId: item.traceId,
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

  async viewDetail(id: string): Promise<void> {
    if (this.loadingDetail()) return;
    this.loadingDetail.set(true);
    try {
      const detail = await firstValueFrom(
        this.api.post<AuditEventDetail>('su/surt09/detail', { id }),
      );
      this.selectedDetail.set(detail);
    } finally {
      this.loadingDetail.set(false);
    }
  }

  async exportCsv(): Promise<void> {
    const isGlobal = this.scopeMode() === 'global';
    const allowed = isGlobal
      ? this.session.hasGlobal('SURT09.EXPORT')
      : Boolean(this.session.companyId()) && this.session.hasAction('SURT09', 'EXPORT');
    if (!allowed || this.exporting()) return;

    this.exporting.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<AuditExportResult>('su/surt09/export', {
          companyId: isGlobal ? null : this.session.companyId(),
          programCode: this.programCode || null,
          action: this.actionFilter.trim() || null,
          keyword: this.keyword.trim() || null,
          sortField: this.sortBy,
          sortDirection: this.sortDirection,
        }),
      );
      const blob = new Blob([result.csvContent], { type: result.contentType });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = result.fileName;
      anchor.click();
      URL.revokeObjectURL(url);
      this.notices.show(`ส่งออกข้อมูล ${result.totalCount} รายการแล้ว`);
    } finally {
      this.exporting.set(false);
    }
  }

  get activeFilterCount(): number {
    return (this.programCode ? 1 : 0) + (this.scopeMode() === 'global' && this.session.companyId() ? 1 : 0);
  }

  resetFilters(): void {
    this.keyword = '';
    this.programCode = '';
    this.actionFilter = '';
    this.scopeMode.set(
      this.session.hasGlobal('SURT09.VIEW') && !this.session.companyId() ? 'global' : 'company',
    );
    this.selectedDetail.set(null);
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
}


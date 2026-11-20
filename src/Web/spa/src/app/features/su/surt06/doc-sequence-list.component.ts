import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DocumentNumberAllocation, DocumentSequenceDto, Page } from '../../../core/models';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';
import { OrganizationListBase, OrganizationRow } from '../../db/shared/organization-crud';

interface DocSeqTableRow extends OrganizationRow {
  prefix: string;
  resetLabel: string;
  previewNextNumber: string;
}

const RESET_LABELS: Record<string, string> = {
  NONE: 'ไม่รีเซ็ต',
  YEAR: 'รายปี (YYYY)',
  YEAR_MONTH: 'รายเดือน (YYYYMM)',
};

@Component({
  selector: 'app-doc-sequence-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './doc-sequence-list.component.html',
  styleUrl: './doc-sequence-list.component.scss',
})
export class DocSequenceListComponent extends OrganizationListBase<DocSeqTableRow> {
  protected override readonly programCode = 'SURT06';
  protected override readonly apiBase = 'su/surt06';
  protected override readonly routeBase = '/su/surt06';
  protected override readonly entityLabel = 'ชุดเลขที่เอกสาร';
  private seq = 0;

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัส', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อเอกสาร', sortable: true },
    { key: 'prefix', label: 'Prefix', sortable: true, format: 'mono' },
    { key: 'resetLabel', label: 'รอบรีเซ็ต', format: 'muted' },
    { key: 'previewNextNumber', label: 'เลขที่ถัดไป', format: 'mono' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }

  override async load(companyId = this.session.companyId()): Promise<void> {
    const seq = ++this.seq;
    this.failed.set(false);
    if (!companyId || !this.session.hasAction('SURT06', 'VIEW')) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<DocumentSequenceDto>>('su/surt06/search', this.buildSearchBody(companyId)),
      );
      if (seq === this.seq) {
        this.rows.set(
          result.items.map((item) => ({
            id: item.id,
            code: item.code,
            name: item.name,
            isActive: item.isActive,
            codeLocked: item.codeLocked,
            version: item.version,
            prefix: item.prefix,
            resetLabel: RESET_LABELS[item.resetPolicy] ?? item.resetPolicy,
            previewNextNumber: item.previewNextNumber,
          })),
        );
        this.total.set(result.totalCount);
      }
    } catch {
      if (seq === this.seq) {
        this.failed.set(true);
        this.rows.set([]);
      }
    } finally {
      if (seq === this.seq) this.loading.set(false);
    }
  }

  async allocateNext(id: string): Promise<void> {
    const row = this.rows().find((x) => x.id === id);
    if (!row || !row.isActive || this.saving()) return;
    const confirmed = await this.confirm.ask({
      title: 'ยืนยันการจองเลขที่เอกสารถัดไป',
      message: `ออกเลขที่เอกสารถัดไปสำหรับ ${row.name} (${row.previewNextNumber}) หรือไม่? หลังออกเลขครั้งแรกจะล็อกรหัสและ Prefix ถาวร`,
      confirmText: 'ออกเลขถัดไป',
      type: 'primary',
    });
    if (!confirmed) return;

    this.saving.set(true);
    try {
      const allocation = await firstValueFrom(
        this.api.post<DocumentNumberAllocation>('su/surt06/next', {
          id: row.id,
          version: row.version,
        }),
      );
      await this.load();
      this.notices.show(`ออกเลขที่เอกสารแล้ว: ${allocation.documentNumber}`);
    } catch {
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}


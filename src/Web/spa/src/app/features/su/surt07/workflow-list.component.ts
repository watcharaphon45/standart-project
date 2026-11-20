import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApprovalWorkflowDto, Page } from '../../../core/models';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';
import { OrganizationListBase, OrganizationRow } from '../../db/shared/organization-crud';

interface WorkflowTableRow extends OrganizationRow {
  documentType: string;
  stepCountLabel: string;
}

@Component({
  selector: 'app-workflow-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './workflow-list.component.html',
  styleUrl: './workflow-list.component.scss',
})
export class WorkflowListComponent extends OrganizationListBase<WorkflowTableRow> {
  protected override readonly programCode = 'SURT07';
  protected override readonly apiBase = 'su/surt07';
  protected override readonly routeBase = '/su/surt07';
  protected override readonly entityLabel = 'ขั้นตอนอนุมัติ';
  private seq = 0;

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสขั้นตอน', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อขั้นตอนอนุมัติ', sortable: true },
    { key: 'documentType', label: 'ประเภทเอกสาร', sortable: true },
    { key: 'stepCountLabel', label: 'จำนวนขั้น', format: 'muted' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }

  override async load(companyId = this.session.companyId()): Promise<void> {
    const seq = ++this.seq;
    this.failed.set(false);
    if (!companyId || !this.session.hasAction('SURT07', 'VIEW')) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.api.post<Page<ApprovalWorkflowDto>>('su/surt07/search', this.buildSearchBody(companyId)),
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
            documentType: item.documentType,
            stepCountLabel: `${item.stepCount} ขั้น`,
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
}


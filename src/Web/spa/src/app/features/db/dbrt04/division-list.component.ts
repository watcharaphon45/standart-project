import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Division, LookupOption } from '../../../core/models';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';
import { OrganizationListBase } from '../shared/organization-crud';

@Component({
  selector: 'app-division-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './division-list.component.html',
  styleUrl: './division-list.component.scss',
})
export class DivisionListComponent extends OrganizationListBase<Division> {
  protected override readonly programCode = 'DBRT04';
  protected override readonly apiBase = 'db/dbrt04';
  protected override readonly routeBase = '/db/dbrt04';
  protected override readonly entityLabel = 'ฝ่าย';

  branchId = '';
  readonly branchOptions = signal<readonly SelectOption[]>([{ value: '', label: 'ทุกสาขา' }]);

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสฝ่าย', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อฝ่าย', sortable: true },
    { key: 'branchCode', label: 'สาขา', sortable: true, format: 'mono' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }

  override get activeFilterCount(): number {
    return (this.branchId ? 1 : 0) + (this.active !== '' ? 1 : 0);
  }

  override resetFilters(): void {
    this.keyword = '';
    this.branchId = '';
    this.active = '';
    this.search();
  }

  protected override onCompanyChanged(companyId: string): void {
    this.branchId = '';
    if (!companyId) {
      this.branchOptions.set([{ value: '', label: 'ทุกสาขา' }]);
      return;
    }
    void this.loadBranches(companyId);
  }

  private async loadBranches(companyId: string): Promise<void> {
    try {
      const branches = await firstValueFrom(
        this.api.post<LookupOption[]>('db/dbrt04/options', { companyId }),
      );
      this.branchOptions.set([
        { value: '', label: 'ทุกสาขา' },
        ...branches.map((branch) => ({
          value: branch.id,
          code: branch.code,
          label: branch.name,
          disabledBadge: branch.isActive ? undefined : 'ปิดใช้งาน',
        })),
      ]);
    } catch {
      this.branchOptions.set([{ value: '', label: 'ทุกสาขา' }]);
    }
  }

  protected override buildSearchBody(companyId: string): Record<string, unknown> {
    return {
      ...super.buildSearchBody(companyId),
      branchId: this.branchId || null,
    };
  }
}


import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Department, DepartmentOptions, DivisionLookupOption, LookupOption } from '../../../core/models';
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
  selector: 'app-department-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './department-list.component.html',
  styleUrl: './department-list.component.scss',
})
export class DepartmentListComponent extends OrganizationListBase<Department> {
  protected override readonly programCode = 'DBRT03';
  protected override readonly apiBase = 'db/dbrt03';
  protected override readonly routeBase = '/db/dbrt03';
  protected override readonly entityLabel = 'แผนก';

  readonly branchFilter = signal('');
  divisionId = '';

  private readonly branches = signal<readonly LookupOption[]>([]);
  private readonly divisions = signal<readonly DivisionLookupOption[]>([]);

  readonly branchOptions = computed<readonly SelectOption[]>(() => [
    { value: '', label: 'ทุกสาขา' },
    ...this.branches().map((branch) => ({
      value: branch.id,
      code: branch.code,
      label: branch.name,
      disabledBadge: branch.isActive ? undefined : 'ปิดใช้งาน',
    })),
  ]);

  readonly divisionOptions = computed<readonly SelectOption[]>(() => {
    const branchId = this.branchFilter();
    const branchNames = new Map(this.branches().map((b) => [b.id, `${b.code} · ${b.name}`]));
    const filtered = branchId
      ? this.divisions().filter((division) => division.branchId === branchId)
      : this.divisions();
    return [
      { value: '', label: 'ทุกฝ่าย' },
      ...filtered.map((division) => ({
        value: division.id,
        code: division.code,
        label: division.name,
        group: branchNames.get(division.branchId),
        disabledBadge: division.isActive ? undefined : 'ปิดใช้งาน',
      })),
    ];
  });

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสแผนก', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อแผนก', sortable: true },
    { key: 'branchCode', label: 'สาขา', sortable: true, format: 'mono' },
    { key: 'divisionCode', label: 'ฝ่าย', sortable: true, format: 'mono' },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }

  override get activeFilterCount(): number {
    return (this.branchFilter() ? 1 : 0) + (this.divisionId ? 1 : 0) + (this.active !== '' ? 1 : 0);
  }

  override resetFilters(): void {
    this.keyword = '';
    this.branchFilter.set('');
    this.divisionId = '';
    this.active = '';
    this.search();
  }

  onBranchFilterChange(value: string): void {
    this.branchFilter.set(value);
    if (value && this.divisionId) {
      const current = this.divisions().find((division) => division.id === this.divisionId);
      if (current && current.branchId !== value) {
        this.divisionId = '';
      }
    }
  }

  protected override onCompanyChanged(companyId: string): void {
    this.branchFilter.set('');
    this.divisionId = '';
    if (!companyId) {
      this.branches.set([]);
      this.divisions.set([]);
      return;
    }
    void this.loadOptions(companyId);
  }

  private async loadOptions(companyId: string): Promise<void> {
    try {
      const options = await firstValueFrom(
        this.api.post<DepartmentOptions>('db/dbrt03/options', { companyId }),
      );
      this.branches.set(options.branches);
      this.divisions.set(options.divisions);
    } catch {
      this.branches.set([]);
      this.divisions.set([]);
    }
  }

  protected override buildSearchBody(companyId: string): Record<string, unknown> {
    return {
      ...super.buildSearchBody(companyId),
      branchId: this.branchFilter() || null,
      divisionId: this.divisionId || null,
    };
  }
}


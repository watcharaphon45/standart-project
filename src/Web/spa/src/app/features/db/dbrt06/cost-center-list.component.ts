import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CostCenter } from '../../../core/models';
import {
  ButtonComponent,
  DataTableComponent,
  PageHeaderComponent,
  SelectComponent,
  TableColumn,
  TableToolbarComponent,
} from '../../../shared/ui';
import { OrganizationListBase } from '../shared/organization-crud';

@Component({
  selector: 'app-cost-center-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './cost-center-list.component.html',
  styleUrl: './cost-center-list.component.scss',
})
export class CostCenterListComponent extends OrganizationListBase<CostCenter> {
  protected override readonly programCode = 'DBRT06';
  protected override readonly apiBase = 'db/dbrt06';
  protected override readonly routeBase = '/db/dbrt06';
  protected override readonly entityLabel = 'ศูนย์ต้นทุน';

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสศูนย์ต้นทุน', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อศูนย์ต้นทุน', sortable: true },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }
}


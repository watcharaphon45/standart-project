import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BusinessUnit } from '../../../core/models';
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
  selector: 'app-business-unit-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './business-unit-list.component.html',
  styleUrl: './business-unit-list.component.scss',
})
export class BusinessUnitListComponent extends OrganizationListBase<BusinessUnit> {
  protected override readonly programCode = 'DBRT07';
  protected override readonly apiBase = 'db/dbrt07';
  protected override readonly routeBase = '/db/dbrt07';
  protected override readonly entityLabel = 'หน่วยธุรกิจ';
  protected override readonly requiresCompany = false;

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสหน่วยธุรกิจ', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อหน่วยธุรกิจ', sortable: true },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }
}


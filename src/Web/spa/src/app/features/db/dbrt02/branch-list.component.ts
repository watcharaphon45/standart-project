import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Branch } from '../../../core/models';
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
  selector: 'app-branch-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './branch-list.component.html',
  styleUrl: './branch-list.component.scss',
})
export class BranchListComponent extends OrganizationListBase<Branch> {
  protected override readonly programCode = 'DBRT02';
  protected override readonly apiBase = 'db/dbrt02';
  protected override readonly routeBase = '/db/dbrt02';
  protected override readonly entityLabel = 'สาขา';

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสสาขา', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อสาขา', sortable: true },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }
}


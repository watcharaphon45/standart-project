import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Position } from '../../../core/models';
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
  selector: 'app-position-list',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    TableToolbarComponent,
    SelectComponent,
    ButtonComponent,
    DataTableComponent,
  ],
  templateUrl: './position-list.component.html',
  styleUrl: './position-list.component.scss',
})
export class PositionListComponent extends OrganizationListBase<Position> {
  protected override readonly programCode = 'DBRT05';
  protected override readonly apiBase = 'db/dbrt05';
  protected override readonly routeBase = '/db/dbrt05';
  protected override readonly entityLabel = 'ตำแหน่งงาน';
  protected override readonly requiresCompany = false;

  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัสตำแหน่ง', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อตำแหน่งงาน', sortable: true },
    { key: 'isActive', label: 'สถานะ', sortable: true, format: 'status' },
  ];

  constructor() {
    super();
    this.initAutoLoad();
  }
}


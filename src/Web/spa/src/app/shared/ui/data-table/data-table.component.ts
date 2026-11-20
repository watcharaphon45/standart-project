import { Component, TemplateRef, contentChild, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { ErrorStateComponent } from '../error-state/error-state.component';
import { TablePaginationComponent } from '../table-pagination/table-pagination.component';

export type BadgeTone = 'success' | 'warning' | 'error' | 'info' | 'muted';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  /**
   * status: เปิด/ปิดใช้งาน (boolean) · badge: ค่าตาม tones · code: รหัสตัวหนา monospace
   * mono: monospace · number: ชิดขวา tabular · trace: monospace ย่อพร้อม title · muted: ข้อความรอง
   */
  format?: 'status' | 'badge' | 'code' | 'mono' | 'number' | 'trace' | 'muted';
  /** ใช้กับ format 'badge': ค่าที่ไม่อยู่ใน tones จะแสดงเป็นข้อความรอง */
  tones?: Readonly<Record<string, BadgeTone>>;
}

export interface TableRow {
  id: string;
  [key: string]: string | boolean;
}

@Component({
  selector: 'app-data-table',
  imports: [NgTemplateOutlet, EmptyStateComponent, ErrorStateComponent, TablePaginationComponent],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
})
export class DataTableComponent {
  readonly rows = input.required<readonly TableRow[]>();
  readonly columns = input.required<readonly TableColumn[]>();
  readonly caption = input.required<string>();
  readonly loading = input(false);
  readonly failed = input(false);
  readonly emptyMessage = input('ไม่พบข้อมูล หรือยังไม่มีสิทธิ์ดูข้อมูลที่เลือก');
  readonly errorMessage = input('โหลดข้อมูลไม่ได้');
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(25);
  readonly sortField = input('');
  readonly sortDirection = input('asc');
  readonly sort = output<string>();
  readonly retry = output<void>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly rowActions = contentChild<TemplateRef<{ $implicit: TableRow }>>('rowActions');

  readonly skeletonRows = [1, 2, 3, 4, 5];

  sortValue(key: string): 'ascending' | 'descending' | 'none' {
    return this.sortField() === key
      ? this.sortDirection() === 'asc'
        ? 'ascending'
        : 'descending'
      : 'none';
  }

  sortIcon(key: string): string {
    const value = this.sortValue(key);
    return value === 'ascending' ? '↑' : value === 'descending' ? '↓' : '↕';
  }

  badgeTone(column: TableColumn, value: string | boolean): BadgeTone | null {
    return column.tones?.[String(value)] ?? null;
  }

  isInactive(row: TableRow): boolean {
    return row['isActive'] === false;
  }
}

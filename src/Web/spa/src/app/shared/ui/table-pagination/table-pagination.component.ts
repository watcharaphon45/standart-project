import { Component, computed, input, output } from '@angular/core';
import { SelectComponent, SelectOption } from '../select/select.component';

@Component({
  selector: 'app-table-pagination',
  imports: [SelectComponent],
  templateUrl: './table-pagination.component.html',
  styleUrl: './table-pagination.component.scss',
})
export class TablePaginationComponent {
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(25);
  readonly loading = input(false);
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly pageSizeOptions: readonly SelectOption[] = [
    { value: '10', label: '10' },
    { value: '25', label: '25' },
    { value: '50', label: '50' },
  ];

  readonly pageSizeString = computed(() => String(this.pageSize()));

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / Math.max(1, this.pageSize()))),
  );

  readonly rangeText = computed(() => {
    const total = this.total();
    if (total <= 0) return '0 รายการ';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(total, this.page() * this.pageSize());
    return `แสดง ${start}–${end} จาก ${total} รายการ`;
  });

  /** เลขหน้าที่แสดง โดย null แทนช่วงที่ถูกย่อ (…) */
  readonly pages = computed<readonly (number | null)[]>(() => {
    const last = this.totalPages();
    const current = Math.min(Math.max(1, this.page()), last);
    if (last <= 7) return Array.from({ length: last }, (_, i) => i + 1);
    const start = Math.max(2, current - 1);
    const end = Math.min(last - 1, current + 1);
    const result: (number | null)[] = [1];
    if (start > 2) result.push(null);
    for (let p = start; p <= end; p++) result.push(p);
    if (end < last - 1) result.push(null);
    result.push(last);
    return result;
  });

  goTo(page: number): void {
    if (this.loading() || page < 1 || page > this.totalPages() || page === this.page()) return;
    this.pageChange.emit(page);
  }

  onResize(size: string): void {
    this.pageSizeChange.emit(Number(size));
  }
}

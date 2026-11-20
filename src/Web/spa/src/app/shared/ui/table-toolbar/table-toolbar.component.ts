import { booleanAttribute, Component, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../button/button.component';
import { InputComponent } from '../input/input.component';

let nextToolbarId = 0;

@Component({
  selector: 'app-table-toolbar',
  imports: [FormsModule, InputComponent, ButtonComponent],
  templateUrl: './table-toolbar.component.html',
  styleUrl: './table-toolbar.component.scss',
})
export class TableToolbarComponent {
  readonly panelId = `clean-toolbar-filters-${++nextToolbarId}`;
  readonly keyword = model<string>('');
  readonly searchId = input<string>(`clean-search-${++nextToolbarId}`);
  readonly searchLabel = input<string>('คำค้นหา');
  readonly searchPlaceholder = input<string>('พิมพ์คำค้นหา...');
  readonly loading = input(false, { transform: booleanAttribute });
  readonly hasFilters = input(true, { transform: booleanAttribute });
  readonly activeFilterCount = input<number>(0);
  readonly search = output<void>();
  readonly resetFilters = output<void>();

  readonly filtersOpen = signal(false);

  toggleFilters(): void {
    this.filtersOpen.update((open) => !open);
  }

  onReset(): void {
    this.keyword.set('');
    this.resetFilters.emit();
  }

  onSubmit(): void {
    this.search.emit();
  }
}

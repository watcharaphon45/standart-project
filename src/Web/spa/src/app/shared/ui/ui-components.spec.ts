import { ApplicationRef, Component } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import {
  ButtonComponent,
  CheckboxComponent,
  ConfirmService,
  DataTableComponent,
  DatePickerComponent,
  InputComponent,
  RadioGroupComponent,
  RadioOption,
  SelectComponent,
  SelectOption,
  TableColumn,
  TableRow,
  TableToolbarComponent,
  TextareaComponent,
} from './index';

@Component({
  imports: [DataTableComponent],
  template: `
    <app-data-table
      [rows]="rows"
      [columns]="columns"
      caption="ทดสอบ"
      [loading]="loading"
      [total]="45"
      [page]="2"
      [pageSize]="10"
      sortField="code"
      sortDirection="asc"
      (pageChange)="requestedPage = $event"
    />
  `,
})
class TableHostComponent {
  loading = false;
  requestedPage = 0;
  readonly columns: TableColumn[] = [
    { key: 'code', label: 'รหัส', sortable: true, format: 'code' },
    { key: 'name', label: 'ชื่อ', sortable: true },
    { key: 'lockState', label: 'ล็อก', format: 'badge', tones: { ถูกล็อก: 'error' } },
    { key: 'traceId', label: 'Trace', format: 'trace' },
    { key: 'isActive', label: 'สถานะ', format: 'status' },
  ];
  readonly rows: TableRow[] = [
    { id: '1', code: 'HQ', name: 'สำนักงานใหญ่', lockState: 'ถูกล็อก', traceId: '00-abc-123', isActive: true },
    { id: '2', code: 'BKK', name: 'กรุงเทพ', lockState: 'ปกติ', traceId: '00-def-456', isActive: false },
  ];
}

@Component({
  imports: [ReactiveFormsModule, FormsModule, TableToolbarComponent, SelectComponent],
  template: `
    <app-table-toolbar
      searchId="tb-keyword"
      searchPlaceholder="ค้นหารหัส..."
      [(keyword)]="keyword"
      [activeFilterCount]="status ? 1 : 0"
      (search)="searched = searched + 1"
      (resetFilters)="reset()"
    >
      <app-select
        selectId="tb-status"
        label="สถานะ"
        name="status"
        [options]="options"
        [(ngModel)]="status"
      />
    </app-table-toolbar>
  `,
})
class ToolbarHostComponent {
  keyword = '';
  status = '';
  searched = 0;
  readonly options: readonly SelectOption[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'true', label: 'เปิดใช้งาน' },
  ];

  reset(): void {
    this.status = '';
    this.searched += 1;
  }
}

@Component({
  imports: [
    ReactiveFormsModule,
    ButtonComponent,
    InputComponent,
    SelectComponent,
    TextareaComponent,
    CheckboxComponent,
    RadioGroupComponent,
    DatePickerComponent,
  ],
  template: `
    <form [formGroup]="form">
      <app-input
        inputId="u-name"
        label="ชื่อ"
        formControlName="name"
        [hint]="hint"
        [error]="error"
      />
      <app-input
        inputId="u-pass"
        label="รหัสผ่าน"
        type="password"
        formControlName="password"
      />
      <app-select
        selectId="u-status"
        label="สถานะ"
        [options]="selectOptions"
        formControlName="status"
      />
      <app-textarea textareaId="u-note" label="หมายเหตุ" formControlName="note" />
      <app-checkbox checkboxId="u-active" label="เปิดใช้งาน" formControlName="active" />
      <app-radio-group legend="ขอบเขต" [options]="radioOptions" formControlName="scope" />
      <app-date-picker
        dateId="u-date"
        label="วันที่เริ่ม"
        min="2026-01-01"
        max="2026-12-31"
        formControlName="startDate"
      />
      <app-button
        type="button"
        variant="primary"
        [disabled]="buttonDisabled"
        [loading]="buttonLoading"
        (buttonClick)="clicks = clicks + 1"
      >
        บันทึก
      </app-button>
    </form>
  `,
})
class UiHostComponent {
  hint = 'คำแนะนำ';
  error = '';
  buttonDisabled = false;
  buttonLoading = false;
  clicks = 0;
  readonly selectOptions: readonly SelectOption[] = [
    { value: 'A', label: 'ตัวเลือก A' },
    { value: 'B', label: 'ตัวเลือก B' },
  ];
  readonly radioOptions: readonly RadioOption[] = [
    { value: 'company', label: 'ทั้งบริษัท' },
    { value: 'branch', label: 'รายสาขา', description: 'ครอบคลุมแผนกใต้สาขา' },
  ];
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true }),
    password: new FormControl('', { nonNullable: true }),
    status: new FormControl('A', { nonNullable: true }),
    note: new FormControl('', { nonNullable: true }),
    active: new FormControl(false, { nonNullable: true }),
    scope: new FormControl('company', { nonNullable: true }),
    startDate: new FormControl('2026-11-16', { nonNullable: true }),
  });
}

@Component({
  imports: [ReactiveFormsModule, SelectComponent],
  template: `
    <app-select
      selectId="cb-division"
      label="ฝ่ายสังกัด"
      [options]="options"
      [clearable]="true"
      [formControl]="control"
    />
  `,
})
class ComboboxHostComponent {
  readonly control = new FormControl('', { nonNullable: true });
  readonly options: readonly SelectOption[] = [
    { value: '', label: '-- เลือกฝ่าย --' },
    { value: '1', code: 'OPS', label: 'ฝ่ายปฏิบัติการ', group: 'BKK · กรุงเทพ' },
    { value: '2', code: 'OLD', label: 'ฝ่ายเดิม', group: 'BKK · กรุงเทพ', disabled: true },
    { value: '3', code: 'IT', label: 'ฝ่ายเทคโนโลยี', group: 'BKK · กรุงเทพ' },
  ];
}

describe('Shared UI Custom Components', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiHostComponent, ToolbarHostComponent, TableHostComponent, ComboboxHostComponent],
    }).compileComponents();
  });

  it('renders typed cells, sort direction, inactive rows, skeleton loading and ranged pagination in DataTableComponent', () => {
    const fixture = TestBed.createComponent(TableHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('td .code')?.textContent?.trim()).toBe('HQ');
    expect(root.querySelector('.badge.tone-error')?.textContent?.trim()).toBe('ถูกล็อก');
    expect(root.querySelectorAll('.badge.tone-error').length).toBe(1);
    expect(root.querySelector('.trace')?.getAttribute('title')).toBe('00-abc-123');
    expect(root.querySelectorAll('tr.row-inactive').length).toBe(1);

    const sortedTh = root.querySelector('th[aria-sort="ascending"]') as HTMLElement;
    expect(sortedTh.querySelector('.sort-icon')?.textContent?.trim()).toBe('↑');
    expect(root.querySelector('th[aria-sort="none"] .sort-icon')?.textContent?.trim()).toBe('↕');

    expect(root.querySelector('footer .total')?.textContent?.trim()).toBe('แสดง 11–20 จาก 45 รายการ');
    const current = root.querySelector('.pager-btn.current') as HTMLButtonElement;
    expect(current.getAttribute('aria-current')).toBe('page');
    expect(current.textContent?.trim()).toBe('2');
    const pageFive = Array.from(root.querySelectorAll<HTMLButtonElement>('.pager-btn')).find(
      (btn) => btn.textContent?.trim() === '5',
    )!;
    pageFive.click();
    expect(host.requestedPage).toBe(5);

    host.loading = true;
    fixture.detectChanges();
    expect(root.querySelectorAll('tr.skeleton-row').length).toBe(5);
    expect(root.querySelector('.table-scroll')?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('td .code')).toBeNull();
  });

  it('prevents duplicate clicks when ButtonComponent is disabled or loading', () => {
    const fixture = TestBed.createComponent(UiHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      'app-button button',
    ) as HTMLButtonElement;
    expect(button.type).toBe('button');
    expect(button.classList.contains('primary')).toBe(true);

    button.click();
    expect(host.clicks).toBe(1);

    host.buttonLoading = true;
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
    button.click();
    expect(host.clicks).toBe(1);

    host.buttonLoading = false;
    host.buttonDisabled = true;
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    button.click();
    expect(host.clicks).toBe(1);
  });

  it('supports ControlValueAccessor, aria-describedby, and password toggle in InputComponent', () => {
    const fixture = TestBed.createComponent(UiHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();

    const nameInput = fixture.nativeElement.querySelector('#u-name') as HTMLInputElement;
    expect(nameInput.getAttribute('aria-describedby')).toBe('u-name-help');

    nameInput.value = 'ทดสอบ';
    nameInput.dispatchEvent(new Event('input'));
    expect(host.form.controls.name.value).toBe('ทดสอบ');

    host.error = 'กรุณาระบุข้อมูล';
    fixture.detectChanges();
    expect(nameInput.getAttribute('aria-invalid')).toBe('true');
    expect(nameInput.getAttribute('aria-describedby')).toBe('u-name-help u-name-error');

    const passInput = fixture.nativeElement.querySelector('#u-pass') as HTMLInputElement;
    const toggleBtn = fixture.nativeElement.querySelector(
      '.password-toggle',
    ) as HTMLButtonElement;
    expect(passInput.type).toBe('password');
    expect(toggleBtn.getAttribute('aria-pressed')).toBe('false');

    toggleBtn.click();
    fixture.detectChanges();
    expect(passInput.type).toBe('text');
    expect(toggleBtn.getAttribute('aria-pressed')).toBe('true');
    expect(toggleBtn.getAttribute('aria-label')).toBe('ซ่อนรหัสผ่าน');

    host.form.controls.password.disable();
    fixture.detectChanges();
    expect(passInput.disabled).toBe(true);
    expect(toggleBtn.disabled).toBe(true);
  });

  it('syncs Select, Textarea, Checkbox, RadioGroup, and DatePicker with Reactive Forms', async () => {
    const fixture = TestBed.createComponent(UiHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();

    const selectInput = fixture.nativeElement.querySelector('#u-status') as HTMLInputElement;
    expect(selectInput.getAttribute('role')).toBe('combobox');
    expect(selectInput.getAttribute('aria-expanded')).toBe('false');
    expect(selectInput.getAttribute('aria-controls')).toBeNull();
    expect(selectInput.value).toBe('ตัวเลือก A');

    selectInput.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(selectInput.getAttribute('aria-expanded')).toBe('true');
    const listboxId = selectInput.getAttribute('aria-controls')!;
    const listbox = document.getElementById(listboxId)!;
    const optionB = Array.from(listbox.querySelectorAll('[role="option"]')).find((el) =>
      el.textContent?.includes('ตัวเลือก B'),
    ) as HTMLElement;
    optionB.click();
    fixture.detectChanges();

    expect(host.form.controls.status.value).toBe('B');
    expect(selectInput.value).toBe('ตัวเลือก B');
    expect(selectInput.getAttribute('aria-expanded')).toBe('false');

    const textarea = fixture.nativeElement.querySelector('#u-note') as HTMLTextAreaElement;
    textarea.value = 'ข้อความบันทึก';
    textarea.dispatchEvent(new Event('input'));
    expect(host.form.controls.note.value).toBe('ข้อความบันทึก');

    const checkbox = fixture.nativeElement.querySelector('#u-active') as HTMLInputElement;
    expect(checkbox.checked).toBe(false);
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));
    expect(host.form.controls.active.value).toBe(true);

    const radios = fixture.nativeElement.querySelectorAll(
      'input[type="radio"]',
    ) as NodeListOf<HTMLInputElement>;
    expect(radios[0].checked).toBe(true);
    radios[1].click();
    radios[1].dispatchEvent(new Event('change'));
    expect(host.form.controls.scope.value).toBe('branch');

    const dateInput = fixture.nativeElement.querySelector('#u-date') as HTMLInputElement;
    expect(dateInput.value).toBe('2026-11-16');
    dateInput.value = '2026-12-01';
    dateInput.dispatchEvent(new Event('input'));
    expect(host.form.controls.startDate.value).toBe('2026-12-01');

    host.form.disable();
    fixture.detectChanges();
    expect(selectInput.disabled).toBe(true);
    expect(textarea.disabled).toBe(true);
    expect(checkbox.disabled).toBe(true);
    expect(radios[0].disabled).toBe(true);
    expect(dateInput.disabled).toBe(true);
  });

  it('supports Direct Editable Combobox filtering, keyboard navigation, disabled skipping, Escape revert, and clearable reset in SelectComponent', () => {
    const fixture = TestBed.createComponent(ComboboxHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('#cb-division') as HTMLInputElement;
    expect(input.value).toBe('');
    expect(input.getAttribute('aria-expanded')).toBe('false');

    // Type to filter by code
    input.value = 'ops';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('true');
    const listbox = document.getElementById(input.getAttribute('aria-controls')!)!;
    expect(listbox.querySelectorAll('[role="option"]').length).toBe(1);
    expect(listbox.querySelector('.ddl-mark')?.textContent?.toUpperCase()).toBe('OPS');

    // Commit highlighted option via Enter
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true }));
    fixture.detectChanges();
    expect(host.control.value).toBe('1');
    expect(input.value).toBe('OPS · ฝ่ายปฏิบัติการ');
    expect(input.getAttribute('aria-expanded')).toBe('false');

    // Non-matching query displays empty state and Escape reverts uncommitted text
    input.value = 'ZZZ';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(document.querySelector('.ddl-empty-title')?.textContent).toContain('ไม่พบรายการที่ตรงกับ');

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    fixture.detectChanges();
    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(input.value).toBe('OPS · ฝ่ายปฏิบัติการ');

    // Open with ArrowDown and skip disabled option to select IT
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', cancelable: true }));
    fixture.detectChanges();
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', cancelable: true }));
    fixture.detectChanges();
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true }));
    fixture.detectChanges();
    expect(host.control.value).toBe('3');
    expect(input.value).toBe('IT · ฝ่ายเทคโนโลยี');

    // Clear button resets value cleanly
    const clearBtn = fixture.nativeElement.querySelector('.clear-btn') as HTMLButtonElement;
    expect(clearBtn).not.toBeNull();
    clearBtn.click();
    fixture.detectChanges();
    expect(host.control.value).toBe('');
    expect(input.value).toBe('');
  });

  it('opens ConfirmDialog with ConfirmDialogData and prevents overlapping dialogs', async () => {
    const fixture = TestBed.createComponent(UiHostComponent);
    fixture.detectChanges();
    const appRef = TestBed.inject(ApplicationRef);
    const confirm = TestBed.inject(ConfirmService);
    const resultPromise = firstValueFrom(
      confirm.confirm({
        title: 'ยืนยันการปิดใช้งาน',
        message: 'ต้องการปิดใช้งานรายการนี้หรือไม่?',
        confirmText: 'ปิดใช้งาน',
        cancelText: 'ยกเลิก',
        type: 'danger',
      }),
    );

    const secondResult = await firstValueFrom(
      confirm.confirm({ message: 'ซ้ำ' }),
    );
    expect(secondResult).toBe(false);

    appRef.tick();
    await fixture.whenStable();

    const dialog = document.querySelector('[role="dialog"]') as HTMLElement;
    expect(dialog).not.toBeNull();
    expect(dialog.textContent).toContain('ยืนยันการปิดใช้งาน');
    expect(dialog.textContent).toContain('ต้องการปิดใช้งานรายการนี้หรือไม่?');

    const confirmBtn = Array.from(dialog.querySelectorAll('button')).find(
      (btn) => btn.textContent?.trim() === 'ปิดใช้งาน',
    )!;
    expect(confirmBtn.classList.contains('danger')).toBe(true);
    confirmBtn.click();
    appRef.tick();

    expect(await resultPromise).toBe(true);
  });

  it('supports compact search, clearable input, collapsible filters drawer, and filter reset in TableToolbarComponent', async () => {
    const fixture = TestBed.createComponent(ToolbarHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    const searchInput = fixture.nativeElement.querySelector('#tb-keyword') as HTMLInputElement;
    const toggleBtn = fixture.nativeElement.querySelector(
      '.filter-toggle-btn button',
    ) as HTMLButtonElement;
    const panel = fixture.nativeElement.querySelector('.filter-panel') as HTMLElement;

    expect(panel.hidden).toBe(true);
    expect(toggleBtn.getAttribute('aria-expanded')).toBe('false');
    expect(toggleBtn.getAttribute('aria-controls')).toBe(panel.id);

    searchInput.value = 'HQ';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(host.keyword).toBe('HQ');

    const clearBtn = fixture.nativeElement.querySelector('.search-clear') as HTMLButtonElement;
    expect(clearBtn).not.toBeNull();
    clearBtn.click();
    fixture.detectChanges();
    expect(host.keyword).toBe('');

    searchInput.value = 'ONE';
    searchInput.dispatchEvent(new Event('input'));
    host.status = 'true';
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('.filter-badge')?.textContent?.trim(),
    ).toBe('1');

    toggleBtn.click();
    fixture.detectChanges();
    expect(panel.hidden).toBe(false);
    expect(toggleBtn.getAttribute('aria-expanded')).toBe('true');

    const submitBtn = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();
    expect(host.searched).toBe(1);

    const resetBtn = panel.querySelector('.filter-foot button') as HTMLButtonElement;
    resetBtn.click();
    fixture.detectChanges();
    expect(host.keyword).toBe('');
    expect(host.status).toBe('');
    expect(host.searched).toBe(2);
  });
});


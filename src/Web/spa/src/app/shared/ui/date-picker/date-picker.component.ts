import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  forwardRef,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextDateId = 0;

@Component({
  selector: 'app-date-picker',
  templateUrl: './date-picker.component.html',
  styleUrl: './date-picker.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true,
    },
  ],
})
export class DatePickerComponent implements ControlValueAccessor {
  readonly dateId = input<string>(`clean-date-${++nextDateId}`);
  readonly label = input<string>('');
  readonly name = input<string>('');
  readonly min = input<string | null>(null);
  readonly max = input<string | null>(null);
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');

  private readonly dateElement = viewChild<ElementRef<HTMLInputElement>>('dateElement');
  private readonly controlDisabled = signal(false);
  readonly value = signal('');

  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));
  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.dateId()}-help`);
    if (this.error()) ids.push(`${this.dateId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    const next = value ?? '';
    this.value.set(next);
    const element = this.dateElement()?.nativeElement;
    if (element && element.value !== next) {
      element.value = next;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.controlDisabled.set(isDisabled);
  }

  onDateChange(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.value.set(next);
    this.onChange(next);
  }

  onBlur(): void {
    this.onTouched();
  }
}


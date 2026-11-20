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

let nextCheckboxId = 0;

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.component.html',
  styleUrl: './checkbox.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CheckboxComponent),
      multi: true,
    },
  ],
})
export class CheckboxComponent implements ControlValueAccessor {
  readonly checkboxId = input<string>(`clean-checkbox-${++nextCheckboxId}`);
  readonly label = input<string>('');
  readonly name = input<string>('');
  readonly indeterminate = input(false, { transform: booleanAttribute });
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');

  private readonly checkboxElement = viewChild<ElementRef<HTMLInputElement>>('checkboxElement');
  private readonly controlDisabled = signal(false);
  readonly checked = signal(false);

  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));
  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.checkboxId()}-help`);
    if (this.error()) ids.push(`${this.checkboxId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  private onChange: (value: boolean) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: boolean | null): void {
    const next = Boolean(value);
    this.checked.set(next);
    const element = this.checkboxElement()?.nativeElement;
    if (element && element.checked !== next) {
      element.checked = next;
    }
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.controlDisabled.set(isDisabled);
  }

  onToggle(event: Event): void {
    const next = (event.target as HTMLInputElement).checked;
    this.checked.set(next);
    this.onChange(next);
  }

  onBlur(): void {
    this.onTouched();
  }
}


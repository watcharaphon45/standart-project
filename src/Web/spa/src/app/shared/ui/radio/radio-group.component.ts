import { booleanAttribute, Component, computed, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface RadioOption {
  value: string;
  label: string;
  description?: string;
  disabled?: boolean;
}

let nextRadioGroupId = 0;

@Component({
  selector: 'app-radio-group',
  templateUrl: './radio-group.component.html',
  styleUrl: './radio-group.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RadioGroupComponent),
      multi: true,
    },
  ],
})
export class RadioGroupComponent implements ControlValueAccessor {
  private readonly instanceId = ++nextRadioGroupId;

  readonly name = input<string>(`clean-radio-${this.instanceId}`);
  readonly legend = input<string>('');
  readonly options = input<readonly RadioOption[]>([]);
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');

  private readonly controlDisabled = signal(false);
  readonly value = signal('');

  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));
  readonly baseId = computed(() => `clean-radio-group-${this.instanceId}`);
  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.baseId()}-help`);
    if (this.error()) ids.push(`${this.baseId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  optionId(index: number): string {
    return `${this.baseId()}-${index}`;
  }

  writeValue(value: string | number | null): void {
    this.value.set(value == null ? '' : String(value));
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

  selectOption(value: string): void {
    if (this.isDisabled()) return;
    this.value.set(value);
    this.onChange(value);
  }

  onBlur(): void {
    this.onTouched();
  }
}


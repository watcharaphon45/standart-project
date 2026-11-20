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

let nextInputId = 0;

@Component({
  selector: 'app-input',
  templateUrl: './input.component.html',
  styleUrl: './input.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
  ],
})
export class InputComponent implements ControlValueAccessor {
  readonly inputId = input<string>(`clean-input-${++nextInputId}`);
  readonly label = input<string>('');
  readonly name = input<string>('');
  readonly type = input<'text' | 'password' | 'email' | 'number' | 'search' | 'tel'>('text');
  readonly placeholder = input<string>('');
  readonly autocomplete = input<string>('');
  readonly maxlength = input<number | string | null>(null);
  readonly required = input(false, { transform: booleanAttribute });
  readonly readonly = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');
  readonly showPasswordToggle = input(true, { transform: booleanAttribute });
  readonly hideLabel = input(false, { transform: booleanAttribute });
  readonly appearance = input<'default' | 'search'>('default');
  readonly clearable = input(false, { transform: booleanAttribute });

  private readonly inputElement = viewChild<ElementRef<HTMLInputElement>>('inputElement');
  private readonly controlDisabled = signal(false);
  readonly value = signal('');
  readonly passwordVisible = signal(false);

  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly hasToggle = computed(() => this.type() === 'password' && this.showPasswordToggle());
  readonly showClear = computed(
    () => this.clearable() && !this.isDisabled() && !this.hasToggle() && this.value().length > 0,
  );
  readonly effectiveType = computed(() =>
    this.type() === 'password' && this.passwordVisible() ? 'text' : this.type(),
  );
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));
  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.inputId()}-help`);
    if (this.error()) ids.push(`${this.inputId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | number | null): void {
    const next = value == null ? '' : String(value);
    this.value.set(next);
    const element = this.inputElement()?.nativeElement;
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

  onInput(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.value.set(next);
    this.onChange(next);
  }

  onBlur(): void {
    this.onTouched();
  }

  togglePassword(): void {
    if (this.isDisabled()) return;
    this.passwordVisible.update((visible) => !visible);
  }

  clearValue(): void {
    if (this.isDisabled()) return;
    this.value.set('');
    const element = this.inputElement()?.nativeElement;
    if (element) {
      element.value = '';
      element.focus();
    }
    this.onChange('');
  }
}


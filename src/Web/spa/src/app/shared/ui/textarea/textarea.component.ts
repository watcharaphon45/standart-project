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

let nextTextareaId = 0;

@Component({
  selector: 'app-textarea',
  templateUrl: './textarea.component.html',
  styleUrl: './textarea.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextareaComponent),
      multi: true,
    },
  ],
})
export class TextareaComponent implements ControlValueAccessor {
  readonly textareaId = input<string>(`clean-textarea-${++nextTextareaId}`);
  readonly label = input<string>('');
  readonly name = input<string>('');
  readonly rows = input<number>(3);
  readonly maxlength = input<number | string | null>(null);
  readonly placeholder = input<string>('');
  readonly required = input(false, { transform: booleanAttribute });
  readonly readonly = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');

  private readonly textareaElement = viewChild<ElementRef<HTMLTextAreaElement>>('textareaElement');
  private readonly controlDisabled = signal(false);
  readonly value = signal('');

  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));
  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.textareaId()}-help`);
    if (this.error()) ids.push(`${this.textareaId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | number | null): void {
    const next = value == null ? '' : String(value);
    this.value.set(next);
    const element = this.textareaElement()?.nativeElement;
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
    const next = (event.target as HTMLTextAreaElement).value;
    this.value.set(next);
    this.onChange(next);
  }

  onBlur(): void {
    this.onTouched();
  }
}


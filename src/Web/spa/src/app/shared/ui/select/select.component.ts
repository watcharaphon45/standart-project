import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  forwardRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
  code?: string;
  group?: string;
  sublabel?: string;
  disabled?: boolean;
  disabledBadge?: string;
}

export interface HighlightSegment {
  text: string;
  match: boolean;
}

let nextSelectId = 0;

const OVERLAY_POSITIONS: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 4 },
  { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 4 },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -4 },
  { originX: 'end', originY: 'top', overlayX: 'end', overlayY: 'bottom', offsetY: -4 },
];

function isPlaceholderOption(option: SelectOption): boolean {
  return option.value === '' && /^--\s*.*\s*--$/.test(option.label.trim());
}

function formatOptionText(option: SelectOption): string {
  return option.code ? `${option.code} · ${option.label}` : option.label;
}

@Component({
  selector: 'app-select',
  imports: [OverlayModule],
  templateUrl: './select.component.html',
  styleUrl: './select.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true,
    },
  ],
})
export class SelectComponent implements ControlValueAccessor {
  readonly selectId = input<string>(`clean-select-${++nextSelectId}`);
  readonly label = input<string>('');
  readonly name = input<string>('');
  readonly ariaLabel = input<string>('');
  readonly placeholder = input<string>('พิมพ์เพื่อค้นหาหรือเลือก...');
  readonly options = input<readonly SelectOption[]>([]);
  readonly value = input<string | null>(null);
  readonly size = input<'md' | 'sm'>('md');
  readonly clearable = input(false, { transform: booleanAttribute });
  readonly panelMinWidth = input<number>(0);
  readonly required = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly invalid = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');

  readonly valueChange = output<string>();

  readonly overlayPositions = OVERLAY_POSITIONS;

  private readonly inputElement = viewChild<ElementRef<HTMLInputElement>>('inputElement');
  private readonly triggerBox = viewChild<ElementRef<HTMLElement>>('triggerBox');
  private readonly listElement = viewChild<ElementRef<HTMLElement>>('listElement');

  private readonly controlDisabled = signal(false);
  private readonly innerValue = signal('');

  readonly isOpen = signal(false);
  readonly isEditingText = signal(false);
  readonly searchQuery = signal('');
  readonly activeIndex = signal(-1);
  readonly triggerWidth = signal(0);

  readonly listboxId = computed(() => `${this.selectId()}-listbox`);
  readonly listboxLabel = computed(() => {
    const name = this.label() || this.ariaLabel();
    return name ? `รายการ${name}` : 'รายการตัวเลือก';
  });
  readonly isControlled = computed(() => this.value() !== null);
  readonly isDisabled = computed(() => this.disabled() || this.controlDisabled());
  readonly currentValue = computed(() => (this.isControlled() ? this.value()! : this.innerValue()));
  readonly isInvalid = computed(() => this.invalid() || Boolean(this.error()));

  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.hint()) ids.push(`${this.selectId()}-help`);
    if (this.error()) ids.push(`${this.selectId()}-error`);
    return ids.length > 0 ? ids.join(' ') : null;
  });

  readonly selectedOption = computed(
    () => this.options().find((opt) => opt.value === this.currentValue()) ?? null,
  );

  readonly effectivePlaceholder = computed(() => {
    const emptyOpt = this.options().find(isPlaceholderOption);
    if (emptyOpt) {
      return emptyOpt.label.replace(/^--\s*|\s*--$/g, '').trim() + '...';
    }
    return this.placeholder();
  });

  readonly committedText = computed(() => {
    const opt = this.selectedOption();
    if (!opt || isPlaceholderOption(opt)) return '';
    return formatOptionText(opt);
  });

  readonly inputDisplayValue = computed(() =>
    this.isEditingText() ? this.searchQuery() : this.committedText(),
  );

  readonly filteredOptions = computed<readonly SelectOption[]>(() => {
    const q = this.searchQuery().trim().toLowerCase();
    const all = this.options();
    if (!q) return all;
    return all.filter((opt) => {
      const code = (opt.code ?? '').toLowerCase();
      const label = opt.label.toLowerCase();
      const group = (opt.group ?? '').toLowerCase();
      const sub = (opt.sublabel ?? '').toLowerCase();
      return code.includes(q) || label.includes(q) || group.includes(q) || sub.includes(q);
    });
  });

  readonly showClearButton = computed(
    () =>
      this.clearable() &&
      !this.isDisabled() &&
      (this.currentValue() !== '' || (this.isEditingText() && this.searchQuery().trim() !== '')),
  );

  readonly computedPanelMinWidth = computed(() =>
    Math.max(this.triggerWidth(), this.panelMinWidth()),
  );

  readonly activeOptionId = computed(() => {
    const idx = this.activeIndex();
    const list = this.filteredOptions();
    if (idx < 0 || idx >= list.length || list[idx].disabled) return null;
    return this.optionDomId(idx);
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | number | null): void {
    const next = value == null ? '' : String(value);
    this.innerValue.set(next);
    if (!this.isOpen()) {
      this.isEditingText.set(false);
      this.searchQuery.set('');
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
    if (isDisabled) this.close(true);
  }

  optionDomId(index: number): string {
    return `${this.selectId()}-opt-${index}`;
  }

  shouldShowGroupHeader(index: number): boolean {
    const list = this.filteredOptions();
    const currentGroup = list[index]?.group;
    return Boolean(currentGroup) && currentGroup !== list[index - 1]?.group;
  }

  highlightSegments(text: string): readonly HighlightSegment[] {
    const q = this.searchQuery().trim();
    if (!q || !this.isEditingText()) return [{ text, match: false }];
    const idx = text.toLowerCase().indexOf(q.toLowerCase());
    if (idx === -1) return [{ text, match: false }];
    const segments: HighlightSegment[] = [];
    if (idx > 0) segments.push({ text: text.slice(0, idx), match: false });
    segments.push({ text: text.slice(idx, idx + q.length), match: true });
    if (idx + q.length < text.length) {
      segments.push({ text: text.slice(idx + q.length), match: false });
    }
    return segments;
  }

  onInputFocus(): void {
    if (this.isEffectivelyDisabled() || this.isOpen()) return;
    this.inputElement()?.nativeElement.select?.();
  }

  onInputClick(): void {
    if (this.isEffectivelyDisabled() || this.isOpen()) return;
    this.open(false);
    queueMicrotask(() => {
      if (!this.isEditingText()) {
        this.inputElement()?.nativeElement.select?.();
      }
    });
  }

  onInput(event: Event): void {
    if (this.isEffectivelyDisabled()) return;
    const text = (event.target as HTMLInputElement).value;
    this.isEditingText.set(true);
    this.searchQuery.set(text);
    if (!this.isOpen()) {
      this.open(true);
    }
    const firstEnabled = this.filteredOptions().findIndex((opt) => !opt.disabled);
    this.activeIndex.set(firstEnabled);
    this.scrollActiveIntoView();
  }

  onKeydown(event: KeyboardEvent): void {
    if (this.isEffectivelyDisabled()) return;

    if (!this.isOpen()) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        this.open(false);
      }
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.moveActive(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActive(-1);
        break;
      case 'Home':
        event.preventDefault();
        this.jumpEdge(false);
        break;
      case 'End':
        event.preventDefault();
        this.jumpEdge(true);
        break;
      case 'Enter': {
        event.preventDefault();
        const option = this.filteredOptions()[this.activeIndex()];
        if (option && !option.disabled) {
          this.selectOption(option);
        }
        break;
      }
      case 'Escape':
        event.preventDefault();
        event.stopPropagation();
        this.close(true);
        break;
      case 'Tab':
        this.close(true);
        this.onTouched();
        break;
    }
  }

  onInputBlur(): void {
    this.onTouched();
    if (this.isOpen()) {
      this.close(true);
    }
  }

  onOutsideClick(event: MouseEvent): void {
    const triggerEl = this.triggerBox()?.nativeElement;
    if (triggerEl && event.target instanceof Node && triggerEl.contains(event.target)) {
      return;
    }
    this.close(true);
    this.onTouched();
  }

  onClearClick(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.isEffectivelyDisabled()) return;
    if (!this.isControlled()) {
      this.innerValue.set('');
      this.onChange('');
    }
    this.onTouched();
    this.valueChange.emit('');
    this.close(true);
    this.inputElement()?.nativeElement.focus?.();
  }

  onChevronMouseDown(event: MouseEvent): void {
    event.preventDefault();
    if (this.isEffectivelyDisabled()) return;
    if (this.isOpen()) {
      this.close(true);
    } else {
      this.inputElement()?.nativeElement.focus?.();
      this.open(false);
    }
  }

  selectOption(option: SelectOption): void {
    if (option.disabled || this.isEffectivelyDisabled()) return;
    if (!this.isControlled()) {
      this.innerValue.set(option.value);
      this.onChange(option.value);
    }
    this.onTouched();
    this.valueChange.emit(option.value);
    this.close(true);
    this.inputElement()?.nativeElement.focus?.();
  }

  close(revertText = true): void {
    if (this.isOpen()) {
      this.isOpen.set(false);
    }
    this.activeIndex.set(-1);
    if (revertText) {
      this.isEditingText.set(false);
      this.searchQuery.set('');
    }
    const el = this.inputElement()?.nativeElement;
    const target = this.inputDisplayValue();
    if (el && el.value !== target) {
      el.value = target;
    }
  }

  private open(preserveQuery: boolean): void {
    if (this.isEffectivelyDisabled()) return;
    this.measureTrigger();
    if (!preserveQuery) {
      this.isEditingText.set(false);
      this.searchQuery.set('');
    }
    this.isOpen.set(true);
    const list = this.filteredOptions();
    const selectedIdx = list.findIndex((o) => o.value === this.currentValue() && !o.disabled);
    const fallbackIdx = list.findIndex((o) => !o.disabled);
    this.activeIndex.set(selectedIdx >= 0 ? selectedIdx : fallbackIdx);
    this.scrollActiveIntoView();
  }

  private isEffectivelyDisabled(): boolean {
    return this.isDisabled() || Boolean(this.inputElement()?.nativeElement.disabled);
  }

  private measureTrigger(): void {
    const width = this.triggerBox()?.nativeElement.getBoundingClientRect().width ?? 0;
    this.triggerWidth.set(Math.round(width));
  }

  private moveActive(step: number): void {
    const list = this.filteredOptions();
    if (list.length === 0) return;
    let next = this.activeIndex();
    for (let i = 0; i < list.length; i++) {
      next = (next + step + list.length) % list.length;
      if (!list[next].disabled) {
        this.activeIndex.set(next);
        this.scrollActiveIntoView();
        return;
      }
    }
  }

  private jumpEdge(toEnd: boolean): void {
    const list = this.filteredOptions();
    if (list.length === 0) return;
    for (let i = 0; i < list.length; i++) {
      const idx = toEnd ? list.length - 1 - i : i;
      if (!list[idx].disabled) {
        this.activeIndex.set(idx);
        this.scrollActiveIntoView();
        return;
      }
    }
  }

  private scrollActiveIntoView(): void {
    queueMicrotask(() => {
      const activeId = this.activeOptionId();
      const container = this.listElement()?.nativeElement;
      if (!activeId || !container) return;
      const optionEl = container.querySelector<HTMLElement>(`[id="${activeId}"]`);
      optionEl?.scrollIntoView?.({ block: 'nearest' });
    });
  }
}

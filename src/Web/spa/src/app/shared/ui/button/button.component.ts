import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

export type ButtonVariant = 'secondary' | 'primary' | 'danger' | 'ghost';

@Component({
  selector: 'app-button',
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  host: {
    '(click)': 'onHostClick($event)',
  },
})
export class ButtonComponent {
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly variant = input<ButtonVariant>('secondary');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly loading = input(false, { transform: booleanAttribute });
  readonly ariaLabel = input<string>('');
  readonly ariaExpanded = input<boolean | null>(null);
  readonly ariaControls = input<string | null>(null);
  readonly align = input<'center' | 'between'>('center');
  readonly titleText = input<string>('');
  readonly buttonClick = output<MouseEvent>();

  private readonly buttonEl = viewChild<ElementRef<HTMLButtonElement>>('btn');

  readonly isDisabled = computed(() => this.disabled() || this.loading());
  readonly variantClass = computed(() => this.variant());

  focus(): void {
    this.buttonEl()?.nativeElement.focus();
  }

  onHostClick(event: MouseEvent): void {
    if (this.isDisabled()) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }

  onInnerClick(event: MouseEvent): void {
    if (this.isDisabled()) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }
    this.buttonClick.emit(event);
  }
}


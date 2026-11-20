import { booleanAttribute, Component, input, output } from '@angular/core';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-error-state',
  imports: [ButtonComponent],
  templateUrl: './error-state.component.html',
  styleUrl: './error-state.component.scss',
})
export class ErrorStateComponent {
  readonly message = input<string>('โหลดข้อมูลไม่ได้');
  readonly retryText = input<string>('ลองอีกครั้ง');
  readonly showRetry = input(true, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly retry = output<void>();
}


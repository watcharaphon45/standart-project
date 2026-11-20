import { booleanAttribute, Component, input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  templateUrl: './form-field.component.html',
  styleUrl: './form-field.component.scss',
})
export class FormFieldComponent {
  readonly forId = input<string>('');
  readonly label = input<string>('');
  readonly required = input(false, { transform: booleanAttribute });
  readonly hint = input<string>('');
  readonly error = input<string>('');
}


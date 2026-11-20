import { FormControl, FormGroup, Validators } from '@angular/forms';
import { FormErrorService } from './form-error.service';
describe('Server field errors', () => {
  it('maps nested validation properties and clears errors after correction', () => {
    const form = new FormGroup({
      name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    });
    const service = new FormErrorService();
    service.apply(form, { errors: { 'Input.Name': ['กรุณาระบุชื่อ'], Unknown: ['ignored'] } });
    expect(service.message(form, 'name')).toBe('กรุณาระบุชื่อ');
    expect(form.controls.name.touched).toBe(true);
    form.controls.name.setValue('บริษัท');
    expect(form.valid).toBe(true);
  });
});

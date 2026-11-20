import { Injectable } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { ApiProblem } from './models';

@Injectable({ providedIn: 'root' })
export class FormErrorService {
  apply(form: FormGroup, problem: ApiProblem): void {
    for (const [property, messages] of Object.entries(problem.errors ?? {})) {
      const name = property.split('.').at(-1) ?? property;
      const key = name.charAt(0).toLowerCase() + name.slice(1);
      const control = form.get(key);
      if (control) {
        control.setErrors({ ...control.errors, server: messages.join(' ') });
        control.markAsTouched();
      }
    }
  }
  message(form: FormGroup, name: string): string {
    const control = form.get(name);
    if (!control?.touched || !control.errors) return '';
    if (control.hasError('server')) return String(control.getError('server'));
    if (control.hasError('required')) return 'กรุณาระบุข้อมูล';
    if (control.hasError('maxlength')) return 'ข้อมูลยาวเกินกำหนด';
    if (control.hasError('minlength')) return 'รหัสผ่านต้องยาวอย่างน้อย 15 ตัวอักษร';
    if (control.hasError('pattern')) return 'ใช้ตัวอักษรอังกฤษ ตัวเลข - หรือ _ เท่านั้น';
    return 'กรุณาตรวจสอบข้อมูล';
  }
}

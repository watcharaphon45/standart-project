import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { FormErrorService } from '../../core/form-error.service';
import { ButtonComponent, InputComponent } from '../../shared/ui';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, InputComponent, ButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly errors = inject(FormErrorService);
  readonly form = inject(FormBuilder).nonNullable.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required],
  });
  readonly busy = signal(false);
  readonly error = signal('');

  async submit(): Promise<void> {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set('');
    try {
      const values = this.form.getRawValue();
      await this.auth.login(values.identifier, values.password);
      this.form.controls.password.reset();
      await this.router.navigateByUrl(
        this.auth.session.user()?.mustChangePassword ? '/change-password' : '/db/dbrt01',
      );
    } catch {
      this.error.set('เข้าใช้ไม่ได้ กรุณาตรวจสอบบัญชี รหัสผ่าน หรือการเชื่อมต่อ หากบัญชีถูกล็อกให้ลองใหม่ภายหลัง');
    } finally {
      this.busy.set(false);
    }
  }
}

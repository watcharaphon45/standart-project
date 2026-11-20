import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { FormErrorService } from '../../core/form-error.service';
import { NotificationService } from '../../core/notification.service';
import { SessionState } from '../../core/session-state.service';
import { ButtonComponent, InputComponent } from '../../shared/ui';

@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule, RouterLink, InputComponent, ButtonComponent],
  templateUrl: './change-password.component.html',
  styleUrl: './login.component.scss',
})
export class ChangePasswordComponent {
  readonly session = inject(SessionState);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notices = inject(NotificationService);
  readonly errors = inject(FormErrorService);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(15), Validators.maxLength(128)]],
    confirmation: ['', Validators.required],
  });

  async submit(): Promise<void> {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (this.form.invalid || value.newPassword !== value.confirmation) {
      this.error.set('กรุณากรอกให้ครบ รหัสใหม่ยาว 15–128 ตัวอักษร และยืนยันรหัสให้ตรงกัน');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      await this.auth.changePassword(value.currentPassword, value.newPassword);
      this.form.reset();
      this.notices.show('เปลี่ยนรหัสผ่านแล้ว กรุณาเข้าสู่ระบบใหม่');
      await this.router.navigateByUrl('/login');
    } catch {
      this.error.set(
        'เปลี่ยนรหัสไม่ได้ กรุณาตรวจรหัสเดิมและเลือกรหัสใหม่ที่ไม่พบบ่อย หากบริการตรวจรหัสไม่พร้อมให้ลองใหม่ภายหลัง',
      );
    } finally {
      this.busy.set(false);
    }
  }
}

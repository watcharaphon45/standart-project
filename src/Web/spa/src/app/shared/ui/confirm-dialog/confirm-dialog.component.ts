import { Component, Injectable, inject } from '@angular/core';
import { DIALOG_DATA, Dialog, DialogRef } from '@angular/cdk/dialog';
import { Observable, defaultIfEmpty, finalize, firstValueFrom, map, of } from 'rxjs';
import { ButtonComponent, ButtonVariant } from '../button/button.component';
import { ConfirmDialogData } from './confirm-dialog.model';

@Component({
  selector: 'app-confirm',
  imports: [ButtonComponent],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmComponent {
  private readonly raw = inject<ConfirmDialogData | string>(DIALOG_DATA);
  readonly data: ConfirmDialogData =
    typeof this.raw === 'string' ? { message: this.raw } : this.raw;
  readonly ref = inject(DialogRef<boolean>);

  readonly title = this.data.title ?? 'ยืนยันการทำรายการ';
  readonly message = this.data.message;
  readonly confirmText = this.data.confirmText ?? 'ยืนยัน';
  readonly cancelText = this.data.cancelText ?? 'ยกเลิก';
  readonly confirmVariant: ButtonVariant = this.data.type === 'danger' ? 'danger' : 'primary';
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly dialog = inject(Dialog);
  private opened = false;

  async ask(input: string | ConfirmDialogData = 'ยืนยันการทำรายการนี้หรือไม่?'): Promise<boolean> {
    return firstValueFrom(this.confirm(typeof input === 'string' ? { message: input } : input));
  }

  confirm(data: ConfirmDialogData): Observable<boolean> {
    if (this.opened) return of(false);
    this.opened = true;
    try {
      return this.dialog
        .open<boolean>(ConfirmComponent, {
          data,
          ariaLabel: data.title ?? 'ยืนยันการทำรายการ',
          width: 'min(440px, 92vw)',
          disableClose: false,
        })
        .closed.pipe(
          map((result) => result === true),
          defaultIfEmpty(false),
          finalize(() => {
            this.opened = false;
          }),
        );
    } catch (error) {
      this.opened = false;
      throw error;
    }
  }
}


import { Injectable, inject, signal } from '@angular/core';
import { ConfirmService } from '../shared/confirm.component';
import { SessionState } from './session-state.service';
@Injectable({ providedIn: 'root' })
export class PendingChanges {
  readonly dirty = signal(false);
  readonly busy = signal(false);
  private readonly confirm = inject(ConfirmService);
  private readonly session = inject(SessionState);
  async leave(): Promise<boolean> {
    if (!this.session.user()) {
      this.dirty.set(false);
      return true;
    }
    if (this.busy()) return false;
    if (!this.dirty()) return true;
    if (!(await this.confirm.ask('มีข้อมูลที่ยังไม่บันทึก ต้องการออกจากหน้านี้หรือไม่?'))) return false;
    this.dirty.set(false);
    return true;
  }
}

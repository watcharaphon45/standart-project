import { Injectable, signal } from '@angular/core';

export type NoticeType = 'success' | 'error' | 'warning' | 'info';

export interface Notice {
  id: number;
  message: string;
  error: boolean;
  type?: NoticeType;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly notices = signal<Notice[]>([]);
  private nextId = 0;

  show(message: string, error = false, type?: NoticeType): void {
    const id = ++this.nextId;
    const resolvedType: NoticeType = type ?? (error ? 'error' : 'success');
    this.notices.update((items) => [
      ...items.slice(-1),
      { id, message, error: error || resolvedType === 'error', type: resolvedType },
    ]);
    setTimeout(() => this.dismiss(id), 8000);
  }

  success(message: string): void {
    this.show(message, false, 'success');
  }

  error(message: string): void {
    this.show(message, true, 'error');
  }

  warning(message: string): void {
    this.show(message, false, 'warning');
  }

  info(message: string): void {
    this.show(message, false, 'info');
  }

  dismiss(id: number): void {
    this.notices.update((items) => items.filter((item) => item.id !== id));
  }
}

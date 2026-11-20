import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { SessionState } from './session-state.service';
import { CurrentUser } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  readonly session = inject(SessionState);
  private initializing?: Promise<void>;

  async csrf(): Promise<void> {
    const response = await firstValueFrom(this.api.get<{ token: string }>('auth/csrf', true));
    this.session.csrfToken = response.token;
  }
  async ensure(): Promise<void> {
    if (this.session.user()) return;
    if (!this.initializing)
      this.initializing = this.initialize().finally(() => {
        this.initializing = undefined;
      });
    await this.initializing;
  }
  private async initialize(): Promise<void> {
    await this.csrf();
    try {
      await this.refresh();
    } catch {
      this.session.clear();
    }
  }
  async refresh(): Promise<void> {
    this.session.accept(await firstValueFrom(this.api.get<CurrentUser>('auth/me', true)));
  }
  async login(identifier: string, password: string): Promise<void> {
    await this.csrf();
    await firstValueFrom(this.api.post('auth/login', { identifier, password }, true));
    await this.csrf();
    await this.refresh();
  }
  async logout(): Promise<void> {
    await firstValueFrom(this.api.post('auth/logout', {}));
    this.session.clear();
  }
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await firstValueFrom(
      this.api.post('auth/change-password', { currentPassword, newPassword }, true),
    );
    this.session.clear();
  }
}

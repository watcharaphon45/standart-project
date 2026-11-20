import { Injectable, computed, signal } from '@angular/core';
import { CurrentUser } from './models';

const GLOBAL_ONLY_PERMISSIONS = new Set([
  'DBRT01.CREATE',
  'DBRT05.CREATE',
  'DBRT05.UPDATE',
  'DBRT05.ACTIVATE',
  'DBRT05.DEACTIVATE',
  'DBRT07.CREATE',
  'DBRT07.UPDATE',
  'DBRT07.ACTIVATE',
  'DBRT07.DEACTIVATE',
  'SURT01.VIEW',
  'SURT01.CREATE',
  'SURT01.UPDATE',
  'SURT01.RESET',
  'SURT01.UNLOCK',
  'SURT01.ACTIVATE',
  'SURT01.DISABLE',
  'SURT05.VIEW',
  'SURT05.UPDATE',
  'SURT05.ACTIVATE',
  'SURT05.DEACTIVATE',
]);

@Injectable({ providedIn: 'root' })
export class SessionState {
  readonly user = signal<CurrentUser | null>(null);
  readonly companyId = signal('');
  csrfToken = '';
  readonly permissions = computed(
    () =>
      this.user()?.companyAccess.find((item) => item.companyId === this.companyId())?.permissions ??
      [],
  );

  has(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasGlobal(permission: string): boolean {
    return this.user()?.globalPermissions.includes(permission) ?? false;
  }

  hasPermission(permission: string): boolean {
    if (GLOBAL_ONLY_PERMISSIONS.has(permission)) {
      return this.hasGlobal(permission);
    }
    return this.hasGlobal(permission) || this.has(permission);
  }

  canCreate(programCode: string): boolean {
    return this.hasPermission(`${programCode}.CREATE`);
  }

  hasAction(programCode: string, action: string): boolean {
    return this.hasPermission(`${programCode}.${action}`);
  }

  setEditingId(programCode: string, id: string): void {
    sessionStorage.setItem(`clean.edit.${programCode}`, id);
  }

  editingId(programCode: string): string {
    return sessionStorage.getItem(`clean.edit.${programCode}`) ?? '';
  }

  accept(user: CurrentUser): void {
    this.user.set(user);
    const stored = sessionStorage.getItem('clean.company.' + user.id);
    const current = this.companyId() || stored;
    this.selectCompany(
      user.companies.some((company) => company.id === current)
        ? current!
        : (user.companies[0]?.id ?? ''),
    );
  }

  selectCompany(id: string): void {
    if (id && !this.user()?.companies.some((company) => company.id === id)) return;
    this.companyId.set(id);
    const user = this.user();
    if (user && id) sessionStorage.setItem('clean.company.' + user.id, id);
  }

  clear(): void {
    this.user.set(null);
    this.companyId.set('');
    this.csrfToken = '';
    for (let index = sessionStorage.length - 1; index >= 0; index--) {
      const key = sessionStorage.key(index);
      if (key?.startsWith('clean.edit.')) sessionStorage.removeItem(key);
    }
  }
}

import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { PendingChanges } from './pending-changes.service';

export const authGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  try {
    await auth.ensure();
  } catch {
    return router.parseUrl('/login');
  }
  const user = auth.session.user();
  if (!user) return router.parseUrl('/login');
  if (user.mustChangePassword && route.routeConfig?.path !== 'change-password')
    return router.parseUrl('/change-password');
  return true;
};

export const pendingGuard: CanDeactivateFn<unknown> = () => inject(PendingChanges).leave();

export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const permission = route.data['permission'] as string;
  if (auth.session.hasPermission(permission)) return true;
  const fallback = auth.session.user()?.navigation[0]?.route ?? '/db/dbrt01';
  return inject(Router).parseUrl(fallback);
};

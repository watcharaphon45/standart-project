import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ApiConfig } from './api-config.service';
import { QUIET_ERROR } from './api.service';
import { SessionState } from './session-state.service';
import { NotificationService } from './notification.service';
import { ApiProblem } from './models';

export const apiInterceptor: HttpInterceptorFn = (request, next) => {
  const config = inject(ApiConfig);
  const session = inject(SessionState);
  const notices = inject(NotificationService);
  const router = inject(Router);
  if (!request.url.startsWith(config.apiBaseUrl + '/api/')) return next(request);
  const unsafe = !['GET', 'HEAD', 'OPTIONS'].includes(request.method);
  const headers: Record<string, string> =
    unsafe && session.csrfToken ? { 'X-CSRF-TOKEN': session.csrfToken } : {};
  return next(request.clone({ withCredentials: true, setHeaders: headers })).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem = error.error as ApiProblem | null;
      if (!request.context.get(QUIET_ERROR)) {
        if (error.status === 401) {
          session.clear();
          void router.navigateByUrl('/login');
        } else if (
          !(error.status === 400 && problem?.errors) &&
          problem?.code !== 'CONCURRENCY_CONFLICT'
        ) {
          notices.show(
            error.status === 0
              ? 'เชื่อมต่อระบบไม่ได้ กรุณาตรวจสอบผลรายการก่อนลองใหม่'
              : problem?.detail || problem?.title || 'ไม่สามารถทำรายการได้ กรุณาลองใหม่',
            true,
          );
        }
      }
      return throwError(() => error);
    }),
  );
};

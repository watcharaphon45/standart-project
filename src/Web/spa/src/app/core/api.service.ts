import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiConfig } from './api-config.service';

export const QUIET_ERROR = new HttpContextToken<boolean>(() => false);
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);
  get<T>(path: string, quiet = false) {
    return this.http.get<T>(this.config.apiBaseUrl + '/api/' + path, {
      context: new HttpContext().set(QUIET_ERROR, quiet),
    });
  }
  post<T>(path: string, body: unknown, quiet = false) {
    return this.http.post<T>(this.config.apiBaseUrl + '/api/' + path, body, {
      context: new HttpContext().set(QUIET_ERROR, quiet),
    });
  }
  put<T>(path: string, body: unknown) {
    return this.http.put<T>(this.config.apiBaseUrl + '/api/' + path, body);
  }
}

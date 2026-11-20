import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ApiConfig } from './api-config.service';
import { apiInterceptor } from './api.interceptor';
import { ApiService } from './api.service';
import { SessionState } from './session-state.service';
import { HttpClient } from '@angular/common/http';
describe('API credentials and CSRF boundary', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    TestBed.inject(ApiConfig).apiBaseUrl = 'https://api.example.test';
    TestBed.inject(SessionState).csrfToken = 'request-token';
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());
  it('adds CSRF only to API writes and includes cookies', () => {
    TestBed.inject(ApiService).post('db/dbrt01/search', {}).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://api.example.test/api/db/dbrt01/search',
    );
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('request-token');
    expect(request.request.withCredentials).toBe(true);
    request.flush({});
  });
  it('never leaks API credentials to another origin', () => {
    TestBed.inject(HttpClient).post('https://external.example.test/api/data', {}).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://external.example.test/api/data',
    );
    expect(request.request.headers.has('X-CSRF-TOKEN')).toBe(false);
    expect(request.request.withCredentials).toBe(false);
    request.flush({});
  });
});

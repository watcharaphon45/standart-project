import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiConfig {
  apiBaseUrl = '';
  async load(): Promise<void> {
    const response = await fetch('/config.json', { cache: 'no-store' });
    if (!response.ok) throw new Error('Cannot load API configuration');
    const config: { apiBaseUrl: string } = await response.json();
    const url = new URL(config.apiBaseUrl);
    if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash)
      throw new Error('API must use an explicit HTTPS origin');
    this.apiBaseUrl = url.origin;
  }
}

import { InjectionToken } from '@angular/core';

export interface AppConfig {
  readonly apiBaseUrl: string;
  readonly pollIntervalMs: number;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

/**
 * `apiBaseUrl` is relative on purpose: the dev proxy and the production host both forward `/api/*`
 * to the API, so the browser only ever makes same-origin calls and CORS never becomes a production
 * concern.
 */
export const defaultAppConfig: AppConfig = {
  apiBaseUrl: '/api/v1',
  pollIntervalMs: 15_000,
};

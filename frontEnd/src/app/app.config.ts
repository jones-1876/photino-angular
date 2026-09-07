import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Hash routing: Photino.NET.Server serves static files with no SPA fallback,
    // so a reload on a path-routed deep link would 404 in Release builds.
    provideRouter(routes, withHashLocation())
  ]
};

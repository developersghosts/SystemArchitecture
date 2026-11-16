import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { TranslatedTitleStrategy } from './core/i18n/translated-title.strategy';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { languageInterceptor } from './core/interceptors/language.interceptor';
import { AppConfigService } from './core/services/app-config.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    // Order matters: language + token are added first, errors are handled last.
    provideHttpClient(withFetch(), withInterceptors([languageInterceptor, authInterceptor, errorInterceptor])),
    { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
    // Toastr notifications for every app message (see ToastService)
    provideToastr({
      closeButton: true,
      progressBar: true,
      preventDuplicates: true,
      includeTitleDuplicates: true,
      newestOnTop: true,
      maxOpened: 4,
      autoDismiss: true,
      tapToDismiss: true,
      enableHtml: false // messages are shown as text, never as HTML
    }),
    // Company, colors, fonts, default language/theme before the first screen is drawn.
    provideAppInitializer(() => inject(AppConfigService).load())
  ]
};

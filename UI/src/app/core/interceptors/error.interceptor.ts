import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';
import { ToastService } from '../services/toast.service';
import { apiErrorMessage } from '../utils/api-error';

/**
 * Handles errors that are the same everywhere:
 * 401 -> the session ended (logout on another device, token revoked or expired): sign out.
 * 403, 5xx and network errors -> toast. 400/429 are left to the screen that made the call.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const language = inject(LanguageService);
  const toast = inject(ToastService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && request.url.startsWith(environment.apiUrl)) {
        const isLogout = request.url.endsWith('/api/Account/logout');

        if (error.status === 401 && !isLogout && auth.session()) {
          auth.endSession(true); // the login page shows "session ended"
        } else if (error.status === 403 || error.status === 0 || error.status >= 500) {
          toast.error(apiErrorMessage(error, language));
        }
      }
      return throwError(() => error);
    })
  );
};

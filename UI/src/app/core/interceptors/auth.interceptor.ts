import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/** Adds "Authorization: Bearer <token>" to requests going to our API only (never to other sites). */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token;
  if (!token || !request.url.startsWith(environment.apiUrl)) return next(request);

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

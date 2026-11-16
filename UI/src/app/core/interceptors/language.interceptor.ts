import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LanguageService } from '../services/language.service';

/** Sends Accept-Language so the API returns messages and validation errors in the UI language. */
export const languageInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiUrl)) return next(request);

  const language = inject(LanguageService).language();
  return next(request.clone({ setHeaders: { 'Accept-Language': language } }));
};

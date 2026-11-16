import { HttpErrorResponse } from '@angular/common/http';
import { LanguageService } from '../services/language.service';

/** ProblemDetails written by BuildingBlocks CustomExceptionHandler. */
interface ApiProblem {
  title?: string;
  detail?: string;
  errorCode?: string;
  ValidationErrors?: { propertyName: string; errorMessage: string }[];
  validationErrors?: { propertyName: string; errorMessage: string }[];
}

export function apiErrorCode(error: unknown): string | null {
  return error instanceof HttpErrorResponse ? ((error.error as ApiProblem | null)?.errorCode ?? null) : null;
}

/** A message to show the user. The API already localizes it from the Accept-Language header. */
export function apiErrorMessage(error: unknown, language: LanguageService): string {
  if (!(error instanceof HttpErrorResponse)) return language.t('errors.unexpected');
  if (error.status === 0) return language.t('errors.network');
  if (error.status === 429) return language.t('errors.tooManyRequests');
  if (error.status === 403) return language.t('errors.forbidden');

  const problem = (typeof error.error === 'object' ? error.error : null) as ApiProblem | null;
  const validation = problem?.ValidationErrors ?? problem?.validationErrors;
  if (validation?.length) return [...new Set(validation.map((v) => v.errorMessage))].join(' ');
  if (error.status >= 500) return language.t('errors.unexpected');
  return problem?.detail || problem?.title || language.t('errors.unexpected');
}

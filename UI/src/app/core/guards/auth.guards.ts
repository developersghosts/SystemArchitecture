import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';
import { ToastService } from '../services/toast.service';

/**
 * Signed-in users only. canMatch also stops the lazy chunk from being downloaded.
 * Sends guests to the login page with returnUrl so they come back after signing in.
 */
export const authGuard: CanMatchFn = (_route, segments) => {
  if (inject(AuthService).isAuthenticated()) return true;

  const returnUrl = '/' + segments.map((s) => s.path).join('/');
  return inject(Router).createUrlTree(['/auth/login'], {
    queryParams: returnUrl === '/' ? {} : { returnUrl }
  });
};

/**
 * Login/OTP pages are for guests only; signed-in users go to the dashboard.
 * Except the reset-password page: its link comes from an email and may be opened in a browser where someone
 * (e.g. the administrator) is still signed in; sending them to the dashboard made the link "not open".
 */
export const guestGuard: CanMatchFn = (_route, segments) => {
  if (segments.some((s) => s.path === 'reset-password')) return true;
  return inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/dashboard']) : true;
};

/** The OTP page needs step 1 (user name + password) first; after a page reload it is gone. */
export const otpGuard: CanActivateFn = () =>
  inject(AuthService).pendingLogin() ? true : inject(Router).createUrlTree(['/auth/login']);

/** At least one of the roles. Use as canMatch: [roleGuard('Administrator')]. */
export function roleGuard(...roles: string[]): CanMatchFn {
  return () => {
    if (inject(AuthService).hasAnyRole(roles)) return true;
    inject(ToastService).error(inject(LanguageService).t('errors.forbidden'));
    return inject(Router).createUrlTree(['/dashboard']);
  };
}

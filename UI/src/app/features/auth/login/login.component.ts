import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorCode, apiErrorMessage } from '../../../core/utils/api-error';
import { DialogComponent } from '../../../shared/dialog/dialog.component';
import { IconComponent } from '../../../shared/icon/icon.component';

/** Step 1: user name + password. The API emails a 6-digit code, then we go to /auth/verify-otp. */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, IconComponent, DialogComponent, TranslatePipe],
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  /** Query params, bound by withComponentInputBinding(). */
  readonly returnUrl = input<string>();
  readonly expired = input<string>();
  readonly passwordChanged = input<string>();
  readonly reset = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    userName: ['', [Validators.required, Validators.maxLength(256)]],
    password: ['', [Validators.required]]
  });

  protected readonly loading = signal(false);
  protected readonly alreadyLoggedIn = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  ngOnInit(): void {
    // Why we are here (from a redirect), shown as a toast instead of a fixed box
    if (this.passwordChanged()) this.toast.success(this.language.t('login.passwordChanged'));
    else if (this.reset()) this.toast.success(this.language.t('login.passwordReset'));
    else if (this.expired()) this.toast.info(this.language.t('errors.sessionExpired'));
  }

  protected submit(forceLogin = false): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.auth.login({ ...this.form.getRawValue(), forceLogin }).subscribe({
      next: () => {
        this.loading.set(false);
        this.alreadyLoggedIn.set(null);
        void this.router.navigate(['/auth/verify-otp'], {
          queryParams: this.safeReturnUrl() ? { returnUrl: this.safeReturnUrl() } : {}
        });
      },
      error: (err: unknown) => {
        this.loading.set(false);
        // Signed in elsewhere: ask in a dialog whether to take over the session
        if (apiErrorCode(err) === 'USER_ALREADY_LOGGED_IN') this.alreadyLoggedIn.set(apiErrorMessage(err, this.language));
        else this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  /** Only paths inside this app (blocks "//evil.com" style redirects). */
  private safeReturnUrl(): string | null {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : null;
  }
}

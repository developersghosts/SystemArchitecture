import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { PASSWORD_PATTERN, matchValidator } from '../../../core/utils/password';
import { IconComponent } from '../../../shared/icon/icon.component';

/** Opened from the emailed link: /auth/reset-password?email=...&token=... -> POST /api/Account/reset-password. */
@Component({
  selector: 'app-reset-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, IconComponent, TranslatePipe],
  template: `
    <h2 class="auth__title">{{ 'reset.title' | t }}</h2>

    @if (!email() || !token()) {
      <p class="auth__subtitle">{{ 'reset.missingToken' | t }}</p>
      <a class="btn btn--primary btn--block" routerLink="/auth/forgot-password">{{ 'reset.requestNew' | t }}</a>
    } @else {
      <p class="auth__subtitle">{{ 'reset.subtitle' | t: { email: email()! } }}</p>
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="form">
        <label class="field">
          <span class="field__label">{{ 'reset.newPassword' | t }}</span>
          <input class="input" type="password" formControlName="newPassword" autocomplete="new-password" dir="ltr" autofocus
            [attr.aria-invalid]="invalid('newPassword')" />
          @if (invalid('newPassword')) { <span class="field__error">{{ 'users.passwordInvalid' | t }}</span> }
          <small class="muted">{{ 'users.passwordHint' | t }}</small>
        </label>
        <label class="field">
          <span class="field__label">{{ 'reset.confirmPassword' | t }}</span>
          <input class="input" type="password" formControlName="confirmPassword" autocomplete="new-password" dir="ltr"
            [attr.aria-invalid]="invalid('confirmPassword')" />
          @if (invalid('confirmPassword')) { <span class="field__error">{{ 'reset.mismatch' | t }}</span> }
        </label>
        <button type="submit" class="btn btn--primary btn--block" [disabled]="loading()">
          @if (loading()) { <span class="spinner" aria-hidden="true"></span> }
          {{ 'reset.submit' | t }}
        </button>
      </form>
    }

    <div class="auth__footer">
      <a class="link-btn" routerLink="/auth/login">
        <app-icon name="arrowBack" [size]="16" class="flip-rtl" /> {{ 'forgot.backToLogin' | t }}
      </a>
      @if (email() && token()) {
        <a class="link-btn" routerLink="/auth/forgot-password">{{ 'reset.requestNew' | t }}</a>
      }
    </div>
  `
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);

  /** Query params from the emailed link (withComponentInputBinding). */
  readonly email = input<string>();
  readonly token = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: matchValidator('newPassword', 'confirmPassword') }
  );
  protected readonly loading = signal(false);
  private readonly toast = inject(ToastService);

  protected invalid(name: 'newPassword' | 'confirmPassword'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { newPassword, confirmPassword } = this.form.getRawValue();
    this.loading.set(true);
    this.auth.resetPassword(this.email()!, this.token()!, newPassword, confirmPassword).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigate(['/auth/login'], { queryParams: { reset: 1 } });
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

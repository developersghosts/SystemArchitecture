import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { IconComponent } from '../../../shared/icon/icon.component';

/** POST /api/Account/forgot-password. The answer is the same whether the email exists or not. */
@Component({
  selector: 'app-forgot-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, IconComponent, TranslatePipe],
  template: `
    <h2 class="auth__title">{{ 'forgot.title' | t }}</h2>
    <p class="auth__subtitle">{{ 'forgot.subtitle' | t }}</p>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="form">
      <label class="field">
        <span class="field__label">{{ 'forgot.email' | t }}</span>
        <span class="field__control">
          <app-icon name="mail" [size]="18" class="field__icon" />
          <input class="input input--with-icon" type="email" formControlName="email" autocomplete="email" dir="ltr"
            autofocus [attr.aria-invalid]="form.controls.email.touched && form.controls.email.invalid" />
        </span>
        @if (form.controls.email.touched && form.controls.email.invalid) {
          <span class="field__error">{{ 'config.invalidEmail' | t }}</span>
        }
      </label>
      <button type="submit" class="btn btn--primary btn--block" [disabled]="loading()">
        @if (loading()) { <span class="spinner" aria-hidden="true"></span> }
        {{ 'forgot.submit' | t }}
      </button>
    </form>

    <div class="auth__footer">
      <a class="link-btn" routerLink="/auth/login">
        <app-icon name="arrowBack" [size]="16" class="flip-rtl" /> {{ 'forgot.backToLogin' | t }}
      </a>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
  });
  protected readonly loading = signal(false);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.auth.forgotPassword(this.form.controls.email.value.trim()).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success(this.language.t('forgot.sent'));
        void this.router.navigate(['/auth/login']);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

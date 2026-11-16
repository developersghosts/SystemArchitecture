import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { IconComponent } from '../../../shared/icon/icon.component';

const RESEND_SECONDS = 30;

/** Step 2: the 6-digit code. On success the JWT is stored and the user goes to returnUrl or the dashboard. */
@Component({
  selector: 'app-verify-otp',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent, TranslatePipe],
  templateUrl: './verify-otp.component.html'
})
export class VerifyOtpComponent {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  readonly returnUrl = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    otp: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]]
  });

  protected readonly loading = signal(false);
  protected readonly resending = signal(false);
  protected readonly secondsLeft = signal(RESEND_SECONDS);
  private timer?: ReturnType<typeof setInterval>;

  constructor() {
    // "We sent you a code" from the API (already in the company's dialect), as a toast
    const sent = this.auth.pendingLogin()?.message;
    if (sent) this.toast.success(sent);
    this.startCountdown();
    inject(DestroyRef).onDestroy(() => clearInterval(this.timer));
  }

  /** Keeps digits only, so pasting "123 456" works. */
  protected onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 6);
    if (digits !== input.value) this.form.controls.otp.setValue(digits);
    if (digits.length === 6) this.submit();
  }

  protected submit(): void {
    if (this.loading()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    this.auth.verifyOtp(this.form.controls.otp.value).subscribe({
      next: () => {
        this.loading.set(false);
        const url = this.returnUrl();
        void this.router.navigateByUrl(url && url.startsWith('/') && !url.startsWith('//') ? url : '/dashboard');
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
        this.form.controls.otp.reset();
      }
    });
  }

  protected resend(): void {
    this.resending.set(true);
    this.auth.resendOtp().subscribe({
      next: () => {
        this.resending.set(false);
        this.toast.success(this.language.t('otp.resent'));
        this.startCountdown();
      },
      error: (err: unknown) => {
        this.resending.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected changeUser(): void {
    this.auth.cancelPendingLogin();
    void this.router.navigate(['/auth/login']);
  }

  private startCountdown(): void {
    clearInterval(this.timer);
    this.secondsLeft.set(RESEND_SECONDS);
    this.timer = setInterval(() => {
      this.secondsLeft.update((s) => s - 1);
      if (this.secondsLeft() <= 0) clearInterval(this.timer);
    }, 1000);
  }
}

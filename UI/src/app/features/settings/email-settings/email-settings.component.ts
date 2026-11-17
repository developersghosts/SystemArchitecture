import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { IconComponent } from '../../../shared/icon/icon.component';
import { PasswordToggleComponent } from '../../../shared/password-toggle/password-toggle.component';

type Security = 'Auto' | 'SslOnConnect' | 'StartTls' | 'None';

interface EmailSettings {
  source: 'database' | 'configuration';
  mode: 'smtp' | 'pickup' | 'disabled';
  smtpServer: string | null;
  port: number;
  security: Security;
  username: string | null;
  hasPassword: boolean;
  senderEmail: string | null;
  senderName: string | null;
  resetPasswordUrl: string | null;
  pickupDirectory: string | null;
  modifiedAtUtc: string | null;
}

interface DeliveryResult {
  delivered: boolean;
  mode: string;
  detail: string | null;
}

const HOST = /^[A-Za-z0-9.-]+$/;

/** SMTP settings saved in the database (password encrypted, never sent back) + "send a test email". */
@Component({
  selector: 'app-email-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PasswordToggleComponent, ReactiveFormsModule, IconComponent, TranslatePipe, LocalDatePipe],
  templateUrl: './email-settings.component.html'
})
export class EmailSettingsComponent {
  private readonly http = inject(HttpClient);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = `${environment.apiUrl}/api/Configuration`;

  protected readonly securityModes: Security[] = ['Auto', 'SslOnConnect', 'StartTls', 'None'];
  protected readonly settings = signal<EmailSettings | null>(null);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isMailpit = computed(() => this.settings()?.smtpServer?.toLowerCase() === 'mailpit');

  protected readonly form = this.fb.group({
    smtpServer: ['', [Validators.maxLength(255), Validators.pattern(HOST)]],
    port: [587, [Validators.required, Validators.min(1), Validators.max(65535)]],
    security: this.fb.control<Security>('Auto'),
    username: ['', [Validators.maxLength(256)]],
    password: ['', [Validators.maxLength(256)]],
    removePassword: [false],
    senderEmail: ['', [Validators.email, Validators.maxLength(256)]],
    resetPasswordUrl: ['', [Validators.maxLength(500)]]
  });
  protected readonly saving = signal(false);

  protected readonly testForm = this.fb.group({
    to: [inject(AuthService).user()?.email ?? '', [Validators.required, Validators.email]]
  });
  protected readonly sending = signal(false);

  constructor() {
    this.http.get<EmailSettings>(`${this.api}/email-settings`).subscribe({
      next: (s) => this.fill(s),
      error: (err: unknown) => this.loadError.set(apiErrorMessage(err, this.language))
    });
  }

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body = {
      smtpServer: v.smtpServer.trim() || null,
      port: Number(v.port),
      security: v.security,
      username: v.username.trim() || null,
      password: v.removePassword ? null : v.password || null, // empty = keep the saved password
      removePassword: v.removePassword,
      senderEmail: v.senderEmail.trim() || null,
      resetPasswordUrl: v.resetPasswordUrl.trim() || null
    };
    this.saving.set(true);
    this.http.put<EmailSettings>(`${this.api}/email-settings`, body).subscribe({
      next: (s) => {
        this.saving.set(false);
        this.fill(s);
        this.toast.success(this.language.t('email.settingsSaved'));
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected send(): void {
    if (this.testForm.invalid) {
      this.testForm.markAllAsTouched();
      return;
    }
    const to = this.testForm.controls.to.value.trim();
    this.sending.set(true);
    this.http.post<DeliveryResult>(`${this.api}/test-email`, { toEmail: to }).subscribe({
      next: (r) => {
        this.sending.set(false);
        this.showResult(r, to);
      },
      error: (err: unknown) => {
        this.sending.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  /** The result of the test email as a toast: sent / saved as a file / the SMTP error. */
  private showResult(r: DeliveryResult, to: string): void {
    if (!r.delivered) this.toast.error(this.language.t('email.failed', { error: r.detail ?? '' }));
    else if (r.mode === 'pickup') this.toast.success(this.language.t('email.saved', { file: r.detail ?? '' }));
    else this.toast.success(this.language.t('email.sent', { to }));
  }

  /** The form shows what is saved in the database; for the configuration fallback it starts empty. */
  private fill(s: EmailSettings): void {
    this.settings.set(s);
    const fromDb = s.source === 'database';
    this.form.reset({
      smtpServer: fromDb ? (s.smtpServer ?? '') : '',
      port: fromDb ? s.port : 587,
      security: fromDb ? s.security : 'Auto',
      username: fromDb ? (s.username ?? '') : '',
      password: '',
      removePassword: false,
      senderEmail: fromDb ? (s.senderEmail ?? '') : '',
      resetPasswordUrl: s.resetPasswordUrl ?? ''
    });
  }
}

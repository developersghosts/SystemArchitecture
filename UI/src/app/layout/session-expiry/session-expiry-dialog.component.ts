import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ToastService } from '../../core/services/toast.service';
import { apiErrorMessage } from '../../core/utils/api-error';
import { DialogComponent } from '../../shared/dialog/dialog.component';
import { IconComponent } from '../../shared/icon/icon.component';

/**
 * Opens 5 minutes before the sign-in session ends: "renew?" with a countdown.
 * Yes -> POST /api/Account/refresh-session (new token, stays signed in). No / no answer -> signed out at the end.
 */
@Component({
  selector: 'app-session-expiry-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogComponent, IconComponent, TranslatePipe],
  template: `
    <app-dialog [open]="open()" [busy]="renewing()" [title]="'session.title' | t" (closed)="dismiss()">
      <div class="session-expiry">
        <span class="session-expiry__icon"><app-icon name="clock" [size]="28" /></span>
        <div>
          <p>{{ 'session.message' | t }}</p>
          <p class="session-expiry__time" aria-live="polite">{{ countdown() }}</p>
          <p class="muted">{{ 'session.question' | t }}</p>
        </div>
      </div>
      <div dialog-actions>
        <button type="button" class="btn btn--outline" (click)="signOut()" [disabled]="renewing()">
          {{ 'nav.logout' | t }}
        </button>
        <button type="button" class="btn btn--primary" (click)="renew()" [disabled]="renewing()" autofocus>
          @if (renewing()) { <span class="spinner" aria-hidden="true"></span> }
          <app-icon name="refresh" [size]="16" /> {{ 'session.renew' | t }}
        </button>
      </div>
    </app-dialog>
  `
})
export class SessionExpiryDialogComponent {
  private readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  /** Closed with Esc / "x": stays hidden for this session (it still ends at the expiry time). */
  private readonly dismissedFor = signal<number | null>(null);
  protected readonly renewing = signal(false);
  private readonly now = signal(Date.now());

  protected readonly open = computed(() => {
    const session = this.auth.session();
    return this.auth.expiringSoon() && !!session && this.dismissedFor() !== session.expiresAt;
  });

  protected readonly countdown = computed(() => {
    const left = Math.max(0, (this.auth.session()?.expiresAt ?? 0) - this.now());
    const minutes = Math.floor(left / 60_000);
    const seconds = Math.floor((left % 60_000) / 1000);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  });

  constructor() {
    // Tick once a second while the dialog is open.
    let timer: ReturnType<typeof setInterval> | undefined;
    effect(() => {
      clearInterval(timer);
      if (!this.open()) return;
      this.now.set(Date.now());
      timer = setInterval(() => this.now.set(Date.now()), 1000);
    });
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected renew(): void {
    this.renewing.set(true);
    this.auth.refreshSession().subscribe({
      next: () => {
        this.renewing.set(false);
        this.toast.success(this.language.t('session.renewed'));
      },
      error: (err: unknown) => {
        this.renewing.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected signOut(): void {
    this.auth.logout();
  }

  protected dismiss(): void {
    this.dismissedFor.set(this.auth.session()?.expiresAt ?? null);
  }
}

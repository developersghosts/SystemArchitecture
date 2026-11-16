import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalDatePipe } from '../../core/i18n/local-date.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ToastService } from '../../core/services/toast.service';
import { apiErrorMessage } from '../../core/utils/api-error';
import { PASSWORD_PATTERN, matchValidator } from '../../core/utils/password';
import { IconComponent } from '../../shared/icon/icon.component';

/** My details (from the session) and POST /api/Account/change-password. */
@Component({
  selector: 'app-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent, TranslatePipe, LocalDatePipe],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">{{ 'account.title' | t }}</h1>
        <p class="page-subtitle">{{ 'account.subtitle' | t }}</p>
      </div>
    </div>

    <section class="grid-2">
      <article class="card">
        <header class="card__header">
          <h2 class="card__title"><app-icon name="user" [size]="18" /> {{ 'account.details' | t }}</h2>
        </header>
        @if (auth.user(); as user) {
          <dl class="details">
            <dt>{{ 'dashboard.name' | t }}</dt>
            <dd>{{ user.fullName }}</dd>
            <dt>{{ 'login.userName' | t }}</dt>
            <dd dir="ltr">{{ user.userName }}</dd>
            <dt>{{ 'dashboard.email' | t }}</dt>
            <dd dir="ltr">{{ user.email }}</dd>
            <dt>{{ 'dashboard.birthDate' | t }}</dt>
            <dd>{{ user.dateOfBirth ? (user.dateOfBirth | localDate: 'long') : '—' }}</dd>
            <dt>{{ 'dashboard.myRoles' | t }}</dt>
            <dd class="tags">
              @for (role of user.roles; track role) { <span class="tag">{{ role }}</span> }
            </dd>
            <dt>{{ 'dashboard.sessionEnds' | t }}</dt>
            <dd>{{ auth.session()?.expiresAt | localDate }}</dd>
          </dl>
        }
      </article>

      <article class="card">
        <header class="card__header">
          <h2 class="card__title"><app-icon name="lock" [size]="18" /> {{ 'account.changePassword' | t }}</h2>
        </header>
        <p class="muted dialog__hint">{{ 'account.changeHint' | t }}</p>


        <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="form">
          <label class="field">
            <span class="field__label">{{ 'account.currentPassword' | t }}</span>
            <input class="input" type="password" formControlName="currentPassword" autocomplete="current-password" dir="ltr"
              [attr.aria-invalid]="invalid('currentPassword')" />
            @if (invalid('currentPassword')) { <span class="field__error">{{ 'common.required' | t }}</span> }
          </label>
          <label class="field">
            <span class="field__label">{{ 'reset.newPassword' | t }}</span>
            <input class="input" type="password" formControlName="newPassword" autocomplete="new-password" dir="ltr"
              [attr.aria-invalid]="invalid('newPassword')" />
            @if (form.controls.newPassword.hasError('same')) {
              <span class="field__error">{{ 'account.samePassword' | t }}</span>
            } @else if (invalid('newPassword')) {
              <span class="field__error">{{ 'users.passwordInvalid' | t }}</span>
            }
            <small class="muted">{{ 'users.passwordHint' | t }}</small>
          </label>
          <label class="field">
            <span class="field__label">{{ 'reset.confirmPassword' | t }}</span>
            <input class="input" type="password" formControlName="confirmPassword" autocomplete="new-password" dir="ltr"
              [attr.aria-invalid]="invalid('confirmPassword')" />
            @if (invalid('confirmPassword')) { <span class="field__error">{{ 'reset.mismatch' | t }}</span> }
          </label>
          <div>
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              @if (saving()) { <span class="spinner" aria-hidden="true"></span> }
              {{ 'account.submit' | t }}
            </button>
          </div>
        </form>
      </article>
    </section>
  `
})
export class AccountComponent {
  protected readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', Validators.required]
    },
    {
      validators: [
        matchValidator('newPassword', 'confirmPassword'),
        // The API refuses a new password equal to the current one.
        (group) => {
          const next = group.get('newPassword')!;
          const same = !!next.value && next.value === group.get('currentPassword')!.value;
          const errors = { ...(next.errors ?? {}) };
          if (same) errors['same'] = true;
          else delete errors['same'];
          next.setErrors(Object.keys(errors).length ? errors : null);
          return null;
        }
      ]
    }
  );
  protected readonly saving = signal(false);

  protected invalid(name: 'currentPassword' | 'newPassword' | 'confirmPassword'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword } = this.form.getRawValue();
    this.saving.set(true);
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.saving.set(false);
        // The API ended every session: sign in again with the new password.
        this.auth.endSession('passwordChanged');
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

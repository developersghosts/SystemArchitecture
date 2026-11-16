import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AppConfigService } from '../../../core/services/app-config.service';
import { LanguageService } from '../../../core/services/language.service';
import { ThemeService } from '../../../core/services/theme.service';
import { IconComponent } from '../../../shared/icon/icon.component';

/** Centered card with the company branding and the language/theme switches. */
@Component({
  selector: 'app-auth-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, IconComponent, TranslatePipe],
  template: `
    <div class="auth">
      <div class="auth__tools">
        <button type="button" class="chip-btn" (click)="language.toggle()" [attr.aria-label]="'nav.languageLabel' | t">
          <app-icon name="languages" [size]="18" />
          <span>{{ 'nav.language' | t }}</span>
        </button>
        <button type="button" class="icon-btn" (click)="theme.toggle()"
          [attr.aria-label]="(theme.isDark() ? 'nav.lightMode' : 'nav.darkMode') | t">
          <app-icon [name]="theme.isDark() ? 'sun' : 'moon'" />
        </button>
      </div>

      <div class="auth__card card">
        <div class="auth__brand">
          @if (config.config().company.logoUrl; as logo) {
            <img class="auth__logo" [src]="logo" alt="" />
          } @else {
            <span class="auth__logo auth__logo--icon"><app-icon name="shield" [size]="28" /></span>
          }
          <h1 class="auth__company">{{ config.companyName() }}</h1>
        </div>
        <router-outlet />
      </div>
    </div>
  `
})
export class AuthLayoutComponent {
  protected readonly config = inject(AppConfigService);
  protected readonly language = inject(LanguageService);
  protected readonly theme = inject(ThemeService);
}

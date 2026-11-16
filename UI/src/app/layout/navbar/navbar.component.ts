import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ThemeService } from '../../core/services/theme.service';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-navbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, TranslatePipe],
  host: { '(document:click)': 'closeOnOutsideClick($event)', '(document:keydown.escape)': 'menuOpen.set(false)' },
  template: `
    <header class="navbar">
      <button type="button" class="icon-btn" (click)="menuToggled.emit()" [attr.aria-label]="'nav.toggleMenu' | t">
        <app-icon name="menu" />
      </button>

      <div class="navbar__spacer"></div>

      <button type="button" class="chip-btn" (click)="language.toggle()" [attr.aria-label]="'nav.languageLabel' | t">
        <app-icon name="languages" [size]="18" />
        <span>{{ 'nav.language' | t }}</span>
      </button>

      <button type="button" class="icon-btn" (click)="theme.toggle()"
        [attr.aria-label]="(theme.isDark() ? 'nav.lightMode' : 'nav.darkMode') | t"
        [attr.title]="(theme.isDark() ? 'nav.lightMode' : 'nav.darkMode') | t">
        <app-icon [name]="theme.isDark() ? 'sun' : 'moon'" />
      </button>

      <div class="user-menu">
        <button type="button" class="user-menu__trigger" (click)="menuOpen.update(o => !o)"
          [attr.aria-expanded]="menuOpen()" aria-haspopup="menu">
          <span class="avatar">{{ initials() }}</span>
          <span class="user-menu__name">{{ auth.user()?.fullName }}</span>
          <app-icon name="chevronDown" [size]="16" />
        </button>

        @if (menuOpen()) {
          <div class="user-menu__panel" role="menu">
            <div class="user-menu__header">
              <small>{{ 'nav.signedInAs' | t }}</small>
              <strong>{{ auth.user()?.userName }}</strong>
              <small class="muted">{{ auth.user()?.email }}</small>
            </div>
            <a class="user-menu__item user-menu__item--plain" role="menuitem" routerLink="/account" (click)="menuOpen.set(false)">
              <app-icon name="user" [size]="18" />
              {{ 'nav.account' | t }}
            </a>
            <button type="button" class="user-menu__item" role="menuitem" (click)="auth.logout()">
              <app-icon name="logout" [size]="18" class="flip-rtl" />
              {{ 'nav.logout' | t }}
            </button>
          </div>
        }
      </div>
    </header>
  `
})
export class NavbarComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  protected readonly auth = inject(AuthService);
  protected readonly language = inject(LanguageService);
  protected readonly theme = inject(ThemeService);

  readonly menuToggled = output();
  protected readonly menuOpen = signal(false);

  protected readonly initials = computed(() =>
    (this.auth.user()?.fullName ?? '?')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0])
      .join('')
      .toUpperCase()
  );

  protected closeOnOutsideClick(event: MouseEvent): void {
    if (this.menuOpen() && !this.host.nativeElement.querySelector('.user-menu')?.contains(event.target as Node)) {
      this.menuOpen.set(false);
    }
  }
}

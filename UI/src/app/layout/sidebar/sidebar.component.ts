import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { TranslationKey } from '../../core/i18n/translations';
import { Roles } from '../../core/models/auth.models';
import { AppConfigService } from '../../core/services/app-config.service';
import { AuthService } from '../../core/services/auth.service';
import { IconComponent, IconName } from '../../shared/icon/icon.component';

interface MenuItem {
  label: TranslationKey;
  icon: IconName;
  link: string;
  /** Shown only to users with one of these roles. */
  roles?: string[];
}

interface MenuGroup {
  label: TranslationKey;
  items: MenuItem[];
}

const MENU: MenuGroup[] = [
  { label: 'nav.main', items: [{ label: 'nav.dashboard', icon: 'dashboard', link: '/dashboard' }] },
  {
    label: 'nav.identity',
    items: [
      { label: 'nav.users', icon: 'users', link: '/users', roles: [Roles.Administrator, Roles.GetAllUsers] },
      { label: 'nav.roles', icon: 'shield', link: '/roles', roles: [Roles.Administrator, Roles.GetAllRolesList] },
      { label: 'nav.groups', icon: 'layers', link: '/groups', roles: [Roles.Administrator, Roles.GetAllRolesList] },
      { label: 'nav.account', icon: 'user', link: '/account' }
    ]
  },
  {
    label: 'nav.settings',
    items: [
      { label: 'nav.configuration', icon: 'settings', link: '/settings/configuration', roles: [Roles.Administrator] },
      { label: 'nav.email', icon: 'mail', link: '/settings/email', roles: [Roles.Administrator] },
      { label: 'nav.messages', icon: 'languages', link: '/settings/messages', roles: [Roles.Administrator] }
    ]
  }
];

@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, IconComponent, TranslatePipe],
  template: `
    <aside class="sidebar" [attr.aria-label]="'nav.main' | t">
      <a class="sidebar__brand" routerLink="/dashboard">
        @if (config.config().company.logoUrl; as logo) {
          <img class="sidebar__logo" [src]="logo" alt="" />
        } @else {
          <span class="sidebar__logo sidebar__logo--text">{{ initials() }}</span>
        }
        <span class="sidebar__name">{{ config.companyName() }}</span>
      </a>

      <nav class="sidebar__nav">
        @for (group of menu(); track group.label) {
          <div class="sidebar__group">
            <span class="sidebar__group-label">{{ group.label | t }}</span>
            @for (item of group.items; track item.link) {
              <a class="sidebar__link" [routerLink]="item.link" routerLinkActive="is-active"
                [attr.title]="collapsed() ? (item.label | t) : null">
                <app-icon [name]="item.icon" />
                <span class="sidebar__label">{{ item.label | t }}</span>
              </a>
            }
          </div>
        }
      </nav>

      <button type="button" class="sidebar__collapse" (click)="collapseToggled.emit()" [attr.aria-label]="'nav.collapse' | t"
        [attr.aria-expanded]="!collapsed()">
        <app-icon name="chevronsStart" class="flip-rtl" />
        <span class="sidebar__label">{{ 'nav.collapse' | t }}</span>
      </button>
    </aside>
  `
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);
  protected readonly config = inject(AppConfigService);

  readonly collapsed = input(false);
  readonly collapseToggled = output();

  /** Hides items (and empty groups) the user has no role for. */
  protected readonly menu = computed(() => {
    this.auth.roles(); // re-filter when the roles change
    return MENU.map((g) => ({
      ...g,
      items: g.items.filter((i) => !i.roles || this.auth.hasAnyRole(i.roles))
    })).filter((g) => g.items.length > 0);
  });

  protected readonly initials = computed(() =>
    this.config
      .companyName()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0])
      .join('')
      .toUpperCase()
  );
}

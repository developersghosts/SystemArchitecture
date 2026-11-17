import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { TranslationKey } from '../../core/i18n/translations';
import { Roles } from '../../core/models/auth.models';
import { AppConfigService } from '../../core/services/app-config.service';
import { AuthService } from '../../core/services/auth.service';
import { safeStorage } from '../../core/utils/storage';
import { IconComponent, IconName } from '../../shared/icon/icon.component';

interface MenuLink {
  label: TranslationKey;
  icon: IconName;
  link: string;
  /** Shown only to users with one of these roles. */
  roles?: string[];
}

/** A link, or a collapsible item with links under it. */
interface MenuItem extends Partial<MenuLink> {
  label: TranslationKey;
  icon: IconName;
  id?: string;
  children?: MenuLink[];
}

interface MenuGroup {
  label: TranslationKey;
  items: MenuItem[];
}

const MENU: MenuGroup[] = [
  {
    label: 'nav.main',
    items: [
      { label: 'nav.dashboard', icon: 'dashboard', link: '/dashboard' },
      {
        id: 'users',
        label: 'nav.users',
        icon: 'users',
        children: [
          { label: 'nav.usersList', icon: 'user', link: '/users', roles: [Roles.Administrator, Roles.GetAllUsers] },
          { label: 'nav.roles', icon: 'shield', link: '/roles', roles: [Roles.Administrator, Roles.GetAllRolesList] },
          { label: 'nav.groups', icon: 'layers', link: '/groups', roles: [Roles.Administrator, Roles.GetAllRolesList] },
          { label: 'nav.account', icon: 'lock', link: '/account' }
        ]
      }
    ]
  },
  {
    label: 'nav.settings',
    items: [
      {
        id: 'system-settings',
        label: 'nav.systemSettings',
        icon: 'settings',
        children: [
          { label: 'nav.configuration', icon: 'building', link: '/settings/configuration', roles: [Roles.Administrator] },
          { label: 'nav.email', icon: 'mail', link: '/settings/email', roles: [Roles.Administrator] },
          { label: 'nav.messages', icon: 'languages', link: '/settings/messages', roles: [Roles.Administrator] }
        ]
      }
    ]
  }
];

/** Old key that remembered open groups; removed so every visit starts with all groups closed. */
const OLD_OPEN_KEY = 'sa.menuOpen';

@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, IconComponent, TranslatePipe],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly config = inject(AppConfigService);

  readonly collapsed = input(false);
  readonly collapseToggled = output();

  /** Which collapsible items are open. All closed at first; only the user opens them. */
  protected readonly open = signal<string[]>([]);
  private readonly url = signal(this.router.url);

  /** Hides links the user has no role for, and collapsible items left with no links. */
  protected readonly menu = computed(() => {
    this.auth.roles(); // re-filter when the roles change
    const allowed = (l: MenuLink) => !l.roles || this.auth.hasAnyRole(l.roles);
    return MENU.map((g) => ({
      ...g,
      items: g.items
        .map((i) => (i.children ? { ...i, children: i.children.filter(allowed) } : i))
        .filter((i) => (i.children ? i.children.length > 0 : allowed(i as MenuLink)))
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

  constructor() {
    safeStorage.remove(OLD_OPEN_KEY);

    // Track the current page only to highlight its group; groups are never opened automatically.
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe((e) => {
        this.url.set((e as NavigationEnd).urlAfterRedirects);
      });
  }

  protected isOpen(item: MenuItem): boolean {
    return !!item.id && this.open().includes(item.id);
  }

  /** A child of this item is the current page. */
  protected hasActiveChild(item: MenuItem): boolean {
    const url = this.url();
    return !!item.children?.some((c) => url === c.link || url.startsWith(c.link + '/') || url.startsWith(c.link + '?'));
  }

  protected toggle(item: MenuItem): void {
    if (!item.id) return;
    const id = item.id;
    this.open.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }
}

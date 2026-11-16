import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { safeStorage } from '../../core/utils/storage';
import { NavbarComponent } from '../navbar/navbar.component';
import { SessionExpiryDialogComponent } from '../session-expiry/session-expiry-dialog.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

const COLLAPSED_KEY = 'sa.sidebarCollapsed';

/** Shell for signed-in pages: side menu + nav bar + page content. */
@Component({
  selector: 'app-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, NavbarComponent, SessionExpiryDialogComponent],
  template: `
    <div class="shell" [class.shell--collapsed]="collapsed()" [class.shell--mobile-open]="mobileOpen()">
      <app-sidebar [collapsed]="collapsed()" (collapseToggled)="toggleCollapsed()" />
      <button type="button" class="shell__backdrop" aria-hidden="true" tabindex="-1" (click)="mobileOpen.set(false)"></button>
      <div class="shell__main">
        <app-navbar (menuToggled)="toggleMenu()" />
        <main class="shell__content" id="content">
          <router-outlet />
        </main>
      </div>
    </div>
    <app-session-expiry-dialog />
  `
})
export class MainLayoutComponent {
  protected readonly collapsed = signal(safeStorage.get(COLLAPSED_KEY) === '1');
  protected readonly mobileOpen = signal(false);

  constructor() {
    // Close the mobile menu after navigating.
    inject(Router)
      .events.pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.mobileOpen.set(false));
  }

  protected toggleMenu(): void {
    if (window.matchMedia('(max-width: 900px)').matches) this.mobileOpen.update((open) => !open);
    else this.toggleCollapsed();
  }

  protected toggleCollapsed(): void {
    this.collapsed.update((c) => !c);
    safeStorage.set(COLLAPSED_KEY, this.collapsed() ? '1' : '0');
  }
}

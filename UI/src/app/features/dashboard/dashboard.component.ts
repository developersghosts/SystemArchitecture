import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LocalDatePipe } from '../../core/i18n/local-date.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Roles, UserListItem } from '../../core/models/auth.models';
import { AppConfigService } from '../../core/services/app-config.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { IconComponent } from '../../shared/icon/icon.component';
import { DashboardService } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LocalDatePipe, IconComponent, TranslatePipe],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private readonly data = inject(DashboardService);
  protected readonly auth = inject(AuthService);
  protected readonly config = inject(AppConfigService);
  protected readonly language = inject(LanguageService);

  /** Same role rules as the API endpoints, so we don't call what would return 403. */
  protected readonly canSeeUsers = this.auth.hasAnyRole([Roles.Administrator, Roles.GetAllUsers]);
  protected readonly canSeeRoles = this.auth.hasAnyRole([Roles.Administrator, Roles.GetAllRolesList]);

  protected readonly usersCount = signal<number | null>(null);
  protected readonly rolesCount = signal<number | null>(null);
  protected readonly users = signal<UserListItem[]>([]);
  protected readonly loadingUsers = signal(this.canSeeUsers);

  protected readonly firstName = computed(() => this.auth.user()?.fullName.split(' ')[0] ?? '');
  protected readonly moneySample = computed(() => {
    this.language.language();
    return this.config.formatMoney(12500.5);
  });

  constructor() {
    if (this.canSeeUsers) {
      this.data.users().subscribe({
        next: (page) => {
          this.usersCount.set(page.totalCount);
          this.users.set(page.items);
          this.loadingUsers.set(false);
        },
        error: () => this.loadingUsers.set(false)
      });
    }
    if (this.canSeeRoles) {
      this.data.roles().subscribe({ next: (page) => this.rolesCount.set(page.totalCount), error: () => undefined });
    }
  }
}

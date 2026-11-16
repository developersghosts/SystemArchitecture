import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { RoleListItem, Roles, UserGroupListItem, UserListItem } from '../../../core/models/auth.models';
import { GroupsService } from '../../groups/groups.service';
import { UserGroupsDialogComponent } from '../user-groups-dialog/user-groups-dialog.component';
import { AuthService } from '../../../core/services/auth.service';
import { IconComponent } from '../../../shared/icon/icon.component';
import { UserFormDialogComponent } from '../user-form-dialog/user-form-dialog.component';
import { UserRolesDialogComponent } from '../user-roles-dialog/user-roles-dialog.component';
import { UsersService } from '../users.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-users-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    IconComponent,
    TranslatePipe,
    LocalDatePipe,
    UserFormDialogComponent,
    UserRolesDialogComponent,
    UserGroupsDialogComponent
  ],
  templateUrl: './users-list.component.html'
})
export class UsersListComponent {
  private readonly users = inject(UsersService);
  private readonly groupsApi = inject(GroupsService);
  protected readonly auth = inject(AuthService);

  /** Same role rules as the API endpoints. */
  protected readonly canCreate = this.auth.hasAnyRole([Roles.Administrator, Roles.RegisterUser]);
  protected readonly canEdit = this.auth.hasAnyRole([Roles.Administrator, Roles.EditUser]);
  protected readonly canManageRoles = this.auth.hasAnyRole([Roles.Administrator]);

  protected readonly items = signal<UserListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(1);
  protected readonly page = signal(1);
  protected readonly search = signal('');
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly allRoles = signal<RoleListItem[]>([]);
  protected readonly allGroups = signal<UserGroupListItem[]>([]);
  protected readonly formOpen = signal(false);
  protected readonly rolesOpen = signal(false);
  protected readonly groupsOpen = signal(false);
  protected readonly selected = signal<UserListItem | null>(null);

  protected readonly currentUserId = computed(() => this.auth.user()?.id);
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.load();
    if (this.canManageRoles) {
      this.users.allRoles().subscribe({ next: (page) => this.allRoles.set(page.items), error: () => undefined });
      this.loadGroups();
    }
    inject(DestroyRef).onDestroy(() => clearTimeout(this.searchTimer));
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.users.list(this.page(), PAGE_SIZE, this.search()).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.total.set(result.totalCount);
        this.totalPages.set(Math.max(1, result.totalPages));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      }
    });
  }

  /** Waits until typing stops, then searches from page 1. */
  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page.set(1);
      this.load();
    }, 350);
  }

  protected goTo(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    this.load();
  }

  protected openCreate(): void {
    this.selected.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(user: UserListItem): void {
    this.selected.set(user);
    this.formOpen.set(true);
  }

  protected openRoles(user: UserListItem): void {
    this.selected.set(user);
    this.rolesOpen.set(true);
  }

  protected openGroups(user: UserListItem): void {
    this.selected.set(user);
    this.groupsOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.rolesOpen.set(false);
    this.groupsOpen.set(false);
    this.load();
    if (this.canManageRoles) this.loadGroups(); // member counts changed
  }

  private loadGroups(): void {
    this.groupsApi.all().subscribe({ next: (page) => this.allGroups.set(page.items), error: () => undefined });
  }
}

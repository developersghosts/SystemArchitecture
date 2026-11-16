import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleListItem, Roles, UserGroupDetails, UserGroupListItem } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ToastService } from '../../core/services/toast.service';
import { apiErrorMessage } from '../../core/utils/api-error';
import { DialogComponent } from '../../shared/dialog/dialog.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { RolesService } from '../roles/roles.service';
import { GroupsService } from './groups.service';

const PAGE_SIZE = 10;

/** User groups: list, create/edit (name, description, roles), delete. Members are managed from the Users page. */
@Component({
  selector: 'app-groups-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DialogComponent, IconComponent, TranslatePipe],
  templateUrl: './groups-list.component.html'
})
export class GroupsListComponent {
  private readonly groups = inject(GroupsService);
  private readonly rolesApi = inject(RolesService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  /** Changing groups changes permissions: Administrator only (same as the API). */
  protected readonly canManage = inject(AuthService).hasAnyRole([Roles.Administrator]);

  protected readonly items = signal<UserGroupListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(1);
  protected readonly page = signal(1);
  protected readonly search = signal('');
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly allRoles = signal<RoleListItem[]>([]);

  // Create / edit dialog
  protected readonly formOpen = signal(false);
  protected readonly editing = signal<UserGroupDetails | null>(null);
  protected readonly loadingDetails = signal(false);
  protected readonly selectedRoles = signal<string[]>([]);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]]
  });

  // Delete dialog
  protected readonly deleting = signal<UserGroupListItem | null>(null);
  protected readonly deleteBusy = signal(false);

  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.load();
    this.rolesApi.list(1, 50, '').subscribe({ next: (r) => this.allRoles.set(r.items), error: () => undefined });
    inject(DestroyRef).onDestroy(() => clearTimeout(this.searchTimer));
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.groups.list(this.page(), PAGE_SIZE, this.search()).subscribe({
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
    this.editing.set(null);
    this.form.reset();
    this.selectedRoles.set([]);
    this.formOpen.set(true);
  }

  /** Loads the details (members) and opens the dialog. */
  protected openEdit(item: UserGroupListItem): void {
    this.form.reset({ name: item.name, description: item.description ?? '' });
    this.selectedRoles.set(item.roles.map((r) => r.id));
    this.editing.set(null);
    this.loadingDetails.set(true);
    this.formOpen.set(true);
    this.groups.get(item.id).subscribe({
      next: (details) => {
        this.editing.set(details);
        this.selectedRoles.set(details.roles.map((r) => r.id));
        this.loadingDetails.set(false);
      },
      error: (err: unknown) => {
        this.loadingDetails.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected toggleRole(id: string, checked: boolean): void {
    this.selectedRoles.update((ids) => (checked ? [...ids, id] : ids.filter((r) => r !== id)));
  }

  protected invalid(name: 'name' | 'description'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body = { name: v.name.trim(), description: v.description.trim() || null, roleIds: this.selectedRoles() };
    const editing = this.editing();
    const request$ = editing ? this.groups.update(editing.id, body) : this.groups.create(body);

    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.toast.success(this.language.t(editing ? 'groups.updated' : 'groups.created'));
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected confirmDelete(): void {
    const group = this.deleting();
    if (!group) return;
    this.deleteBusy.set(true);
    this.groups.delete(group.id).subscribe({
      next: () => {
        this.deleteBusy.set(false);
        this.deleting.set(null);
        this.toast.success(this.language.t('groups.deleted'));
        if (this.items().length === 1 && this.page() > 1) this.page.update((p) => p - 1);
        this.load();
      },
      error: (err: unknown) => {
        this.deleteBusy.set(false);
        this.deleting.set(null);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

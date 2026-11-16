import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { RoleListItem, Roles, UserListItem } from '../../../core/models/auth.models';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DialogComponent } from '../../../shared/dialog/dialog.component';
import { UsersService } from '../users.service';

/** Tick the roles a user should have; saves with PUT /api/UserRoles/setUserRoles. */
@Component({
  selector: 'app-user-roles-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogComponent, TranslatePipe],
  template: `
    <app-dialog [open]="open()" [busy]="saving()" (closed)="closed.emit()"
      [title]="'users.rolesTitle' | t: { name: user()?.firstName + ' ' + user()?.lastName }">
      <p class="muted dialog__hint">{{ 'users.rolesHint' | t }}</p>

      @if (loading()) {
        <div class="skeleton-list"><span class="skeleton"></span><span class="skeleton"></span><span class="skeleton"></span></div>
      } @else {
        <div class="checks">
          @for (role of roles(); track role.id) {
            @let locked = isLocked(role);
            <label class="check" [class.check--disabled]="locked" [attr.title]="locked ? ('users.ownAdmin' | t) : null">
              <input type="checkbox" [checked]="selected().includes(role.id)" [disabled]="locked || saving()"
                (change)="toggle(role.id, $any($event.target).checked)" />
              <span>
                {{ role.title }} <small class="muted" dir="ltr">({{ role.name }})</small>
                @if (role.description) { <br /><small class="muted">{{ role.description }}</small> }
              </span>
            </label>
          }
        </div>
      }

      <div dialog-actions>
        <button type="button" class="btn btn--outline" (click)="closed.emit()" [disabled]="saving()">{{ 'common.cancel' | t }}</button>
        <button type="button" class="btn btn--primary" (click)="save()" [disabled]="saving() || loading() || !changed()">
          @if (saving()) { <span class="spinner" aria-hidden="true"></span> {{ 'common.saving' | t }} } @else { {{ 'common.save' | t }} }
        </button>
      </div>
    </app-dialog>
  `
})
export class UserRolesDialogComponent {
  private readonly users = inject(UsersService);
  private readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  readonly open = input(false);
  readonly user = input<UserListItem | null>(null);
  readonly roles = input<RoleListItem[]>([]);
  readonly closed = output();
  readonly saved = output();

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly selected = signal<string[]>([]);
  private readonly original = signal<string[]>([]);

  protected readonly changed = computed(() => {
    const a = [...this.selected()].sort().join();
    const b = [...this.original()].sort().join();
    return a !== b;
  });

  constructor() {
    effect(() => {
      const user = this.user();
      if (!this.open() || !user) return;
      this.loading.set(true);
      this.users.userRoles(user.id).subscribe({
        next: (list) => {
          const ids = list.map((r) => r.roleId);
          this.original.set(ids);
          this.selected.set(ids);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.toast.error(apiErrorMessage(err, this.language));
        }
      });
    });
  }

  /** Your own Administrator role stays ticked (the API refuses removing it too). */
  protected isLocked(role: RoleListItem): boolean {
    return role.name === Roles.Administrator && this.user()?.id === this.auth.user()?.id
      && this.original().includes(role.id);
  }

  protected toggle(id: string, checked: boolean): void {
    this.selected.update((ids) => (checked ? [...ids, id] : ids.filter((r) => r !== id)));
  }

  protected save(): void {
    const user = this.user();
    if (!user) return;
    this.saving.set(true);
    this.users.setRoles(user.id, this.selected()).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(this.language.t('users.rolesSaved'));
        this.saved.emit();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

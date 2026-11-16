import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UserGroupListItem, UserListItem } from '../../../core/models/auth.models';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DialogComponent } from '../../../shared/dialog/dialog.component';
import { GroupsService } from '../../groups/groups.service';

/** Tick the groups a user belongs to; saves with PUT /api/UserGroups/setUserGroups. */
@Component({
  selector: 'app-user-groups-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogComponent, TranslatePipe],
  template: `
    <app-dialog [open]="open()" [busy]="saving()" [wide]="true" (closed)="closed.emit()"
      [title]="'users.groupsTitle' | t: { name: user()?.firstName + ' ' + user()?.lastName }">
      <p class="muted dialog__hint">{{ 'users.groupsHint' | t }}</p>

      @if (loading()) {
        <div class="skeleton-list"><span class="skeleton"></span><span class="skeleton"></span></div>
      } @else if (groups().length === 0) {
        <p class="muted">{{ 'users.noGroups' | t }}</p>
      } @else {
        <div class="checks">
          @for (group of groups(); track group.id) {
            <label class="check">
              <input type="checkbox" [checked]="selected().includes(group.id)" [disabled]="saving()"
                (change)="toggle(group.id, $any($event.target).checked)" />
              <span>
                <strong>{{ group.name }}</strong>
                @if (group.description) { <small class="muted"> — {{ group.description }}</small> }
                <span class="tags check__tags">
                  @for (role of group.roles; track role.id) { <span class="tag">{{ role.title }}</span> }
                  @empty { <small class="muted">{{ 'groups.noRoles' | t }}</small> }
                </span>
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
export class UserGroupsDialogComponent {
  private readonly groupsApi = inject(GroupsService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  readonly open = input(false);
  readonly user = input<UserListItem | null>(null);
  readonly groups = input<UserGroupListItem[]>([]);
  readonly closed = output();
  readonly saved = output();

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly selected = signal<string[]>([]);
  private readonly original = signal<string[]>([]);

  protected readonly changed = computed(
    () => [...this.selected()].sort().join() !== [...this.original()].sort().join()
  );

  constructor() {
    effect(() => {
      const user = this.user();
      if (!this.open() || !user) return;
      this.loading.set(true);
      this.groupsApi.ofUser(user.id).subscribe({
        next: (list) => {
          const ids = list.map((g) => g.id);
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

  protected toggle(id: string, checked: boolean): void {
    this.selected.update((ids) => (checked ? [...ids, id] : ids.filter((g) => g !== id)));
  }

  protected save(): void {
    const user = this.user();
    if (!user) return;
    this.saving.set(true);
    this.groupsApi.setUserGroups(user.id, this.selected()).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(this.language.t('users.groupsSaved'));
        this.saved.emit();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language)); // e.g. "would remove your own Administrator role"
      }
    });
  }
}

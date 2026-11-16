import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleListItem, Roles } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ToastService } from '../../core/services/toast.service';
import { apiErrorMessage } from '../../core/utils/api-error';
import { DialogComponent } from '../../shared/dialog/dialog.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { RolesService } from './roles.service';

const PAGE_SIZE = 10;

/** GET /api/Roles/getAllRolesList (paged, search) and POST /api/Roles/createRole. */
@Component({
  selector: 'app-roles-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DialogComponent, IconComponent, TranslatePipe],
  templateUrl: './roles-list.component.html'
})
export class RolesListComponent {
  private readonly roles = inject(RolesService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  protected readonly canCreate = inject(AuthService).hasAnyRole([Roles.Administrator, Roles.CreateRole]);

  protected readonly items = signal<RoleListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(1);
  protected readonly page = signal(1);
  protected readonly search = signal('');
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly dialogOpen = signal(false);
  protected readonly saving = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(256), Validators.pattern(/^[A-Za-z0-9_.-]+$/)]],
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(256)]],
    description: ['', [Validators.maxLength(500)]]
  });

  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.load();
    inject(DestroyRef).onDestroy(() => clearTimeout(this.searchTimer));
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.roles.list(this.page(), PAGE_SIZE, this.search()).subscribe({
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
    this.form.reset();
    this.dialogOpen.set(true);
  }

  protected invalid(name: 'name' | 'title' | 'description'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.roles.create([{ name: v.name.trim(), title: v.title.trim(), description: v.description.trim() || null }]).subscribe({
      next: (created) => {
        this.saving.set(false);
        if (created.length === 0) {
          this.toast.error(this.language.t('roles.exists')); // the API skips existing names
          return;
        }
        this.toast.success(this.language.t('roles.created'));
        this.dialogOpen.set(false);
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { RoleListItem, UserGroupListItem, UserListItem } from '../../../core/models/auth.models';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DialogComponent } from '../../../shared/dialog/dialog.component';
import { UsersService } from '../users.service';

import { PASSWORD_PATTERN as PASSWORD } from '../../../core/utils/password';

/** Create (user = null) or edit a user. */
@Component({
  selector: 'app-user-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DialogComponent, TranslatePipe],
  templateUrl: './user-form-dialog.component.html'
})
export class UserFormDialogComponent {
  private readonly users = inject(UsersService);
  private readonly auth = inject(AuthService);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);

  readonly open = input(false);
  readonly user = input<UserListItem | null>(null);
  /** Roles that can be given on create (Administrator only); empty hides the section. */
  readonly roles = input<RoleListItem[]>([]);
  /** Groups that can be chosen on create (Administrator only). */
  readonly groups = input<UserGroupListItem[]>([]);
  readonly closed = output();
  readonly saved = output();

  protected readonly isEdit = computed(() => !!this.user());
  protected readonly isSelf = computed(() => this.user()?.id === this.auth.user()?.id);
  protected readonly saving = signal(false);
  protected readonly selectedRoles = signal<string[]>([]);
  protected readonly selectedGroups = signal<string[]>([]);

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(256)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    phoneNumber: ['', [Validators.maxLength(30)]],
    dateOfBirth: [''],
    password: [''],
    isActive: [true]
  });

  constructor() {
    // Fill the form each time the dialog opens.
    effect(() => {
      if (!this.open()) return;
      const user = this.user();
      this.selectedRoles.set([]);
      this.selectedGroups.set([]);
      this.form.reset({
        firstName: user?.firstName ?? '',
        lastName: user?.lastName ?? '',
        userName: user?.userName ?? '',
        email: user?.email ?? '',
        phoneNumber: user?.phoneNumber ?? '',
        dateOfBirth: user?.dateOfBirth ?? '',
        password: '',
        isActive: user?.isActive ?? true
      });

      const password = this.form.controls.password;
      password.setValidators(user ? [] : [Validators.required, Validators.pattern(PASSWORD)]);
      password.updateValueAndValidity();

      // Your own account cannot be deactivated (the API refuses it too).
      if (user && user.id === this.auth.user()?.id) this.form.controls.isActive.disable();
      else this.form.controls.isActive.enable();
    });
  }

  protected invalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  protected toggleRole(id: string, checked: boolean): void {
    this.selectedRoles.update((ids) => (checked ? [...ids, id] : ids.filter((r) => r !== id)));
  }

  protected toggleGroup(id: string, checked: boolean): void {
    this.selectedGroups.update((ids) => (checked ? [...ids, id] : ids.filter((g) => g !== id)));
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const common = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      userName: v.userName.trim(),
      email: v.email.trim(),
      phoneNumber: v.phoneNumber.trim() || null,
      dateOfBirth: v.dateOfBirth || null
    };

    const user = this.user();
    const request$ = user
      ? this.users.edit({ id: user.id, ...common, isActive: v.isActive })
      : this.users.create(
          { ...common, password: v.password, roleIds: this.selectedRoles(), groupIds: this.selectedGroups() },
          this.roles().length > 0
        );

    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(this.language.t(user ? 'users.updated' : 'users.created'));
        this.saved.emit();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

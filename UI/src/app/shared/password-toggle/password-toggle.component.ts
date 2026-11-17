import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { IconComponent } from '../icon/icon.component';

/**
 * Eye button that shows / hides a password field. Put it right after the input, inside a
 * `<span class="field__control" dir="ltr">`, and give the input the class `input--with-action`:
 *
 *   <span class="field__control" dir="ltr">
 *     <input #pw type="password" class="input input--with-action" ... />
 *     <app-password-toggle [input]="pw" />
 *   </span>
 */
@Component({
  selector: 'app-password-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent, TranslatePipe],
  template: `
    <button type="button" class="field__action" (click)="toggle()" [attr.aria-pressed]="visible()"
      [attr.aria-label]="(visible() ? 'login.hidePassword' : 'login.showPassword') | t"
      [attr.title]="(visible() ? 'login.hidePassword' : 'login.showPassword') | t">
      <app-icon [name]="visible() ? 'eyeOff' : 'eye'" [size]="18" />
    </button>
  `
})
export class PasswordToggleComponent {
  readonly input = input.required<HTMLInputElement>();
  protected readonly visible = signal(false);

  protected toggle(): void {
    const show = !this.visible();
    this.visible.set(show);
    const field = this.input();
    field.type = show ? 'text' : 'password';
    field.focus();
  }
}

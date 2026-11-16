import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  viewChild
} from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { IconComponent } from '../icon/icon.component';

/**
 * Modal built on the native <dialog> (focus stays inside, Esc closes, works with RTL).
 * <app-dialog [open]="x()" [title]="..." (closed)="x.set(false)"> content + <div dialog-actions> </app-dialog>
 */
@Component({
  selector: 'app-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent, TranslatePipe],
  template: `
    <dialog #dialog class="dialog" [class.dialog--wide]="wide()" (cancel)="onCancel($event)" (click)="onBackdrop($event)"
      [attr.aria-labelledby]="'dialog-title'">
      <div class="dialog__panel">
        <header class="dialog__header">
          <h2 class="dialog__title" id="dialog-title">{{ title() }}</h2>
          <button type="button" class="icon-btn icon-btn--sm" (click)="closed.emit()" [attr.aria-label]="'common.cancel' | t">
            <app-icon name="x" [size]="18" />
          </button>
        </header>
        <div class="dialog__body"><ng-content /></div>
        <footer class="dialog__footer"><ng-content select="[dialog-actions]" /></footer>
      </div>
    </dialog>
  `
})
export class DialogComponent {
  readonly open = input(false);
  readonly title = input('');
  readonly wide = input(false);
  /** Set to true while saving, so Esc / backdrop clicks do not close it. */
  readonly busy = input(false);
  readonly closed = output();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const el = this.dialog().nativeElement;
      if (this.open() && !el.open) el.showModal();
      else if (!this.open() && el.open) el.close();
    });
  }

  protected onCancel(event: Event): void {
    event.preventDefault(); // the parent decides (keeps [open] in sync)
    if (!this.busy()) this.closed.emit();
  }

  protected onBackdrop(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement && !this.busy()) this.closed.emit();
  }
}

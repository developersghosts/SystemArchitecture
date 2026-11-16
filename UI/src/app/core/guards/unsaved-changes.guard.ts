import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { LanguageService } from '../services/language.service';

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/** Asks before leaving a page with unsaved changes (inside the app; page reload/close is handled by the page itself). */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) =>
  !component.hasUnsavedChanges() || confirm(inject(LanguageService).t('config.unsavedConfirm'));

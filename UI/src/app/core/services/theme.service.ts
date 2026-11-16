import { DOCUMENT, Injectable, computed, effect, inject, signal } from '@angular/core';
import { Theme } from '../models/configuration.models';
import { safeStorage } from '../utils/storage';

const STORAGE_KEY = 'sa.theme';

/** Light/dark mode: sets data-theme on <html>; the colors live in styles/_tokens.scss. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly stored = parse(safeStorage.get(STORAGE_KEY));

  readonly theme = signal<Theme>(this.stored ?? 'light');
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    effect(() => {
      const html = this.document.documentElement;
      html.dataset['theme'] = this.theme();
      html.style.colorScheme = this.theme();
    });
  }

  /** Uses the project's default theme unless the user already picked one. */
  applyDefault(theme: Theme): void {
    if (!this.stored) this.theme.set(theme);
  }

  toggle(): void {
    const next: Theme = this.isDark() ? 'light' : 'dark';
    this.theme.set(next);
    safeStorage.set(STORAGE_KEY, next);
  }
}

function parse(value: string | null): Theme | null {
  return value === 'light' || value === 'dark' ? value : null;
}

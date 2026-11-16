import { DOCUMENT, Injectable, computed, effect, inject, signal } from '@angular/core';
import { AR, EN, TranslationKey } from '../i18n/translations';
import { Language } from '../models/configuration.models';
import { safeStorage } from '../utils/storage';

const STORAGE_KEY = 'sa.language';

/** Current language (en/ar), page direction (ltr/rtl) and UI translations. */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly document = inject(DOCUMENT);
  private readonly stored = parse(safeStorage.get(STORAGE_KEY));

  readonly language = signal<Language>(this.stored ?? 'en');
  readonly direction = computed(() => (this.language() === 'ar' ? 'rtl' : 'ltr'));
  readonly isArabic = computed(() => this.language() === 'ar');

  constructor() {
    effect(() => {
      const html = this.document.documentElement;
      html.lang = this.language();
      html.dir = this.direction();
    });
  }

  /** Uses the project's default language unless the user already picked one. */
  applyDefault(language: Language): void {
    if (!this.stored) this.language.set(language);
  }

  use(language: Language): void {
    this.language.set(language);
    safeStorage.set(STORAGE_KEY, language);
  }

  toggle(): void {
    this.use(this.language() === 'ar' ? 'en' : 'ar');
  }

  t(key: TranslationKey, params?: Record<string, string | number>): string {
    let text: string = (this.language() === 'ar' ? AR : EN)[key] ?? key;
    if (params) {
      for (const [name, value] of Object.entries(params)) text = text.replaceAll(`{${name}}`, String(value));
    }
    return text;
  }
}

function parse(value: string | null): Language | null {
  return value === 'en' || value === 'ar' ? value : null;
}

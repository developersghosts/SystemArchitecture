import { HttpClient } from '@angular/common/http';
import { DOCUMENT, Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, firstValueFrom, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DEFAULT_CONFIGURATION, ProjectConfiguration } from '../models/configuration.models';
import { LanguageService } from './language.service';
import { ThemeService } from './theme.service';

const FONT_NAME = /^[A-Za-z0-9 ]{2,60}$/;
const HEX_COLOR = /^#[0-9A-Fa-f]{6}$/;

/**
 * Project configuration from GET /api/Configuration (company, currency, colors, fonts).
 * Loaded once before the app starts and applied as CSS variables on <html>.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);
  private readonly language = inject(LanguageService);
  private readonly theme = inject(ThemeService);
  private readonly url = `${environment.apiUrl}/api/Configuration`;

  readonly config = signal<ProjectConfiguration>(DEFAULT_CONFIGURATION);

  readonly companyName = computed(() => {
    const company = this.config().company;
    return this.language.isArabic() ? company.nameAr : company.nameEn;
  });

  constructor() {
    effect(() => this.apply(this.config(), this.language.isArabic()));
  }

  /** Called by provideAppInitializer; the app still starts with the defaults if the API is down. */
  async load(): Promise<void> {
    try {
      this.config.set(await firstValueFrom(this.http.get<ProjectConfiguration>(this.url)));
    } catch {
      this.config.set(DEFAULT_CONFIGURATION);
    }
    this.language.applyDefault(this.config().defaultLanguage);
    this.theme.applyDefault(this.config().defaultTheme);
  }

  save(configuration: ProjectConfiguration): Observable<ProjectConfiguration> {
    return this.http.put<ProjectConfiguration>(this.url, configuration).pipe(tap((saved) => this.config.set(saved)));
  }

  /** Shows a configuration on screen without saving it (used by the live preview). */
  preview(configuration: ProjectConfiguration): void {
    this.apply(configuration, this.language.isArabic());
  }

  /** Puts the saved configuration back on screen after a preview. */
  restore(): void {
    this.apply(this.config(), this.language.isArabic());
  }

  formatMoney(amount: number, configuration = this.config()): string {
    const { decimalPlaces, symbol } = configuration.currency;
    const value = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: decimalPlaces,
      maximumFractionDigits: decimalPlaces
    }).format(amount);
    return this.language.isArabic() ? `${value} ${symbol}` : `${symbol} ${value}`;
  }

  private apply(configuration: ProjectConfiguration, arabic: boolean): void {
    const style = this.document.documentElement.style;
    const { colors, fonts } = configuration;

    if (HEX_COLOR.test(colors.primary)) {
      style.setProperty('--primary', colors.primary);
      style.setProperty('--on-primary', readableTextColor(colors.primary));
    }
    if (HEX_COLOR.test(colors.lightBackground)) style.setProperty('--bg-light', colors.lightBackground);
    if (HEX_COLOR.test(colors.darkBackground)) style.setProperty('--bg-dark', colors.darkBackground);

    const font = arabic ? fonts.fontFamilyAr : fonts.fontFamilyEn;
    if (FONT_NAME.test(font)) {
      this.loadGoogleFont(font);
      style.setProperty('--font-family', `'${font}'`);
    }
    style.setProperty('--font-size', `${Math.min(20, Math.max(12, fonts.baseFontSize))}px`);
  }

  /** One <link> per family, so a font that fails to load does not block the others. */
  private loadGoogleFont(font: string): void {
    const id = `font-${font.replaceAll(' ', '-').toLowerCase()}`;
    if (this.document.getElementById(id)) return;

    const link = this.document.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = `https://fonts.googleapis.com/css2?family=${font.replaceAll(' ', '+')}:wght@400;700&display=swap`;
    this.document.head.appendChild(link);
  }
}

/** Black or white, whichever reads better on the given color. */
function readableTextColor(hex: string): string {
  const [r, g, b] = [1, 3, 5].map((i) => {
    const c = parseInt(hex.slice(i, i + 2), 16) / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  return luminance > 0.4 ? '#0F172A' : '#FFFFFF';
}

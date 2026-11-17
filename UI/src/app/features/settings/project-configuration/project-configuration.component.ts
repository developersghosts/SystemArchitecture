import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { HasUnsavedChanges } from '../../../core/guards/unsaved-changes.guard';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import {
  ARABIC_FONTS,
  COUNTRY_TIME_ZONES,
  ENGLISH_FONTS,
  Language,
  ProjectConfiguration,
  Theme
} from '../../../core/models/configuration.models';
import { AppConfigService } from '../../../core/services/app-config.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { shrinkImage } from '../../../core/utils/image-resize';
import { IconComponent } from '../../../shared/icon/icon.component';

const HEX = /^#[0-9A-Fa-f]{6}$/;

function withCurrent(fonts: string[], current: string | undefined): string[] {
  return current && !fonts.includes(current) ? [current, ...fonts] : fonts;
}

function urlValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value?.trim();
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? null : { url: true };
  } catch {
    return { url: true };
  }
}

/** Company details, currency, colors, fonts and defaults (GET/PUT /api/Configuration). Changes preview live. */
@Component({
  selector: 'app-project-configuration',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent, TranslatePipe, LocalDatePipe],
  templateUrl: './project-configuration.component.html',
  // Browser warning on reload/close while there are unsaved changes.
  host: { '(window:beforeunload)': 'onBeforeUnload($event)' }
})
export class ProjectConfigurationComponent implements HasUnsavedChanges {
  private readonly appConfig = inject(AppConfigService);
  protected readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly saved = this.appConfig.config;

  protected readonly form = this.fb.group({
    company: this.fb.group({
      nameEn: ['', [Validators.required, Validators.maxLength(200)]],
      nameAr: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.email, Validators.maxLength(256)]],
      phone: ['', [Validators.pattern(/^\+?[0-9 ()-]{5,30}$/)]],
      address: ['', [Validators.maxLength(500)]],
      website: ['', [urlValidator, Validators.maxLength(256)]],
      taxNumber: ['', [Validators.maxLength(50)]],
      logoUrl: [''],
      country: ['']
    }),
    currency: this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
      symbol: ['', [Validators.required, Validators.maxLength(10)]],
      decimalPlaces: [2, [Validators.required, Validators.min(0), Validators.max(4)]]
    }),
    colors: this.fb.group({
      primary: ['', [Validators.required, Validators.pattern(HEX)]],
      lightBackground: ['', [Validators.required, Validators.pattern(HEX)]],
      darkBackground: ['', [Validators.required, Validators.pattern(HEX)]]
    }),
    fonts: this.fb.group({
      fontFamilyEn: ['', Validators.required],
      fontFamilyAr: ['', Validators.required],
      baseFontSize: [14, [Validators.required, Validators.min(12), Validators.max(20)]]
    }),
    defaultLanguage: this.fb.control<Language>('en'),
    defaultTheme: this.fb.control<Theme>('light')
  });

  protected readonly saving = signal(false);
  protected readonly logoError = signal<string | null>(null);
  protected readonly processingLogo = signal(false);
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly logo = computed(() => this.value().company?.logoUrl || null);
  protected readonly previewMoney = computed(() => {
    this.value();
    this.language.language();
    return this.appConfig.formatMoney(1234.5, this.toConfiguration());
  });

  /** Time zone that the chosen country gives, with the current time there (before saving). */
  protected readonly zoneHint = computed(() => {
    const zone = COUNTRY_TIME_ZONES[this.value().company?.country ?? ''];
    if (!zone) return null;
    const time = new Intl.DateTimeFormat(this.appConfig.locale(), { timeStyle: 'short', timeZone: zone }).format(new Date());
    return { zone, time };
  });

  /** The offered fonts, plus the saved one if it is not in the list. */
  protected readonly englishFonts = computed(() => withCurrent(ENGLISH_FONTS, this.value().fonts?.fontFamilyEn));
  protected readonly arabicFonts = computed(() => withCurrent(ARABIC_FONTS, this.value().fonts?.fontFamilyAr));

  constructor() {
    this.reset();

    // Live preview of colors and fonts while editing (not saved until "Save").
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.form.controls.colors.valid && this.form.controls.fonts.valid) {
        this.appConfig.preview(this.toConfiguration());
      }
    });
    inject(DestroyRef).onDestroy(() => this.appConfig.restore());
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.saving();
  }

  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) event.preventDefault();
  }

  protected invalid(path: string): boolean {
    const control = this.form.get(path);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  protected reset(): void {
    const c = this.appConfig.config();
    this.form.reset({
      company: {
        nameEn: c.company.nameEn,
        nameAr: c.company.nameAr,
        email: c.company.email ?? '',
        phone: c.company.phone ?? '',
        address: c.company.address ?? '',
        website: c.company.website ?? '',
        taxNumber: c.company.taxNumber ?? '',
        logoUrl: c.company.logoUrl ?? '',
        country: c.company.country ?? ''
      },
      currency: { ...c.currency },
      colors: { ...c.colors },
      fonts: { ...c.fonts },
      defaultLanguage: c.defaultLanguage,
      defaultTheme: c.defaultTheme
    });
    this.logoError.set(null);
    this.appConfig.restore();
  }

  /** Any image size is accepted: it is resized in the browser (max 512 px, WEBP) before it is saved. */
  protected async onLogoSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.processingLogo.set(true);
    this.logoError.set(null);
    try {
      this.form.controls.company.controls.logoUrl.setValue(await shrinkImage(file));
      this.form.markAsDirty();
    } catch {
      this.logoError.set(this.language.t('config.logoType'));
    } finally {
      this.processingLogo.set(false);
    }
  }

  protected removeLogo(): void {
    this.form.controls.company.controls.logoUrl.setValue('');
    this.form.markAsDirty();
  }

  /** Keeps the text box and the color picker in sync. */
  protected setColor(name: 'primary' | 'lightBackground' | 'darkBackground', event: Event): void {
    const control = this.form.controls.colors.controls[name];
    control.setValue((event.target as HTMLInputElement).value.toUpperCase());
    control.markAsDirty();
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error(this.language.t('config.invalid'));
      return;
    }

    this.saving.set(true);
    this.appConfig.save(this.toConfiguration()).subscribe({
      next: () => {
        this.saving.set(false);
        this.reset();
        this.toast.success(this.language.t('config.saved'));
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  private toConfiguration(): ProjectConfiguration {
    const v = this.form.getRawValue();
    const blank = (s: string) => (s.trim() ? s.trim() : null);
    return {
      company: {
        nameEn: v.company.nameEn.trim(),
        nameAr: v.company.nameAr.trim(),
        email: blank(v.company.email),
        phone: blank(v.company.phone),
        address: blank(v.company.address),
        website: blank(v.company.website),
        taxNumber: blank(v.company.taxNumber),
        logoUrl: blank(v.company.logoUrl),
        country: blank(v.company.country)
      },
      currency: {
        code: v.currency.code.trim().toUpperCase(),
        symbol: v.currency.symbol.trim(),
        decimalPlaces: Number(v.currency.decimalPlaces)
      },
      colors: {
        primary: v.colors.primary.toUpperCase(),
        lightBackground: v.colors.lightBackground.toUpperCase(),
        darkBackground: v.colors.darkBackground.toUpperCase()
      },
      fonts: {
        fontFamilyEn: v.fonts.fontFamilyEn,
        fontFamilyAr: v.fonts.fontFamilyAr,
        baseFontSize: Number(v.fonts.baseFontSize)
      },
      defaultLanguage: v.defaultLanguage,
      defaultTheme: v.defaultTheme
    };
  }
}

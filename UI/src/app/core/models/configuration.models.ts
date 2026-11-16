export type Language = 'en' | 'ar';
export type Theme = 'light' | 'dark';

export interface CompanyDetails {
  nameEn: string;
  nameAr: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  website: string | null;
  taxNumber: string | null;
  logoUrl: string | null;
  /** "EG" Egyptian or "SA" Saudi dialect for the system messages; null = standard Arabic. */
  country?: string | null;
}

export interface CurrencySettings {
  code: string;
  symbol: string;
  decimalPlaces: number;
}

export interface ThemeColors {
  primary: string;
  lightBackground: string;
  darkBackground: string;
}

export interface Fonts {
  fontFamilyEn: string;
  fontFamilyAr: string;
  baseFontSize: number;
}

/** GET/PUT /api/Configuration */
export interface ProjectConfiguration {
  company: CompanyDetails;
  currency: CurrencySettings;
  colors: ThemeColors;
  fonts: Fonts;
  defaultLanguage: Language;
  defaultTheme: Theme;
  modifiedAtUtc?: string | null;
}

/** Used until the API answers, or when it can't be reached. Matches ProjectConfiguration.CreateDefault(). */
export const DEFAULT_CONFIGURATION: ProjectConfiguration = {
  company: {
    nameEn: 'System Architecture',
    nameAr: 'System Architecture',
    email: null,
    phone: null,
    address: null,
    website: null,
    taxNumber: null,
    logoUrl: null
  },
  currency: { code: 'USD', symbol: '$', decimalPlaces: 2 },
  colors: { primary: '#4F46E5', lightBackground: '#F4F6FB', darkBackground: '#0F172A' },
  fonts: { fontFamilyEn: 'Inter', fontFamilyAr: 'Cairo', baseFontSize: 14 },
  defaultLanguage: 'en',
  defaultTheme: 'light'
};

/** Google Fonts offered in the configuration screen. */
export const ENGLISH_FONTS = ['Inter', 'Roboto', 'Poppins', 'Open Sans', 'Lato', 'Montserrat', 'Nunito'];
export const ARABIC_FONTS = ['Cairo', 'Tajawal', 'Almarai', 'IBM Plex Sans Arabic', 'Noto Kufi Arabic', 'Readex Pro', 'Changa'];

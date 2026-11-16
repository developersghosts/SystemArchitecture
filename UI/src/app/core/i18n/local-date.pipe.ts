import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../services/language.service';

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;
const HAS_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

/**
 * Dates in the UI language. {{ value | localDate }} or {{ value | localDate: 'long' }}.
 * API DateTime values are UTC; when the string has no zone it is read as UTC. Date-only values are not shifted.
 */
@Pipe({ name: 'localDate', pure: false })
export class LocalDatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: string | number | null | undefined, style: 'short' | 'long' = 'short'): string {
    if (value === null || value === undefined || value === '') return '';

    const dateOnly = typeof value === 'string' && DATE_ONLY.test(value);
    const date =
      typeof value === 'string' && !dateOnly && !HAS_ZONE.test(value) ? new Date(`${value}Z`) : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const options: Intl.DateTimeFormatOptions = dateOnly
      ? { dateStyle: style === 'long' ? 'long' : 'medium', timeZone: 'UTC' }
      : { dateStyle: style === 'long' ? 'long' : 'medium', timeStyle: 'short' };

    return new Intl.DateTimeFormat(this.language.isArabic() ? 'ar-EG' : 'en-US', options).format(date);
  }
}

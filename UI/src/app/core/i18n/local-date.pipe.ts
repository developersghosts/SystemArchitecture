import { Pipe, PipeTransform, inject } from '@angular/core';
import { AppConfigService } from '../services/app-config.service';

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;
const HAS_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

/**
 * Dates in the UI language, shown in the company's time zone (from the country in Project configuration:
 * Egypt -> Cairo, Saudi Arabia -> Riyadh; no country -> the browser's time zone).
 * {{ value | localDate }} or {{ value | localDate: 'long' }}.
 * API DateTime values are UTC; when the string has no zone it is read as UTC. Date-only values are not shifted.
 */
@Pipe({ name: 'localDate', pure: false })
export class LocalDatePipe implements PipeTransform {
  private readonly config = inject(AppConfigService);

  transform(value: string | number | null | undefined, style: 'short' | 'long' = 'short'): string {
    if (value === null || value === undefined || value === '') return '';

    const dateOnly = typeof value === 'string' && DATE_ONLY.test(value);
    const date =
      typeof value === 'string' && !dateOnly && !HAS_ZONE.test(value) ? new Date(`${value}Z`) : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const options: Intl.DateTimeFormatOptions = dateOnly
      ? { dateStyle: style === 'long' ? 'long' : 'medium', timeZone: 'UTC' }
      : { dateStyle: style === 'long' ? 'long' : 'medium', timeStyle: 'short', timeZone: this.config.timeZone() };

    try {
      return new Intl.DateTimeFormat(this.config.locale(), options).format(date);
    } catch {
      // Unknown time zone in this browser: fall back to the browser's own
      return new Intl.DateTimeFormat(this.config.locale(), { ...options, timeZone: undefined }).format(date);
    }
  }
}

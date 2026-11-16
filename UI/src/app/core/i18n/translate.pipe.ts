import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../services/language.service';
import { TranslationKey } from './translations';

/** {{ 'login.title' | t }} or {{ 'otp.resendIn' | t: { seconds: 30 } }}. Re-evaluates when the language changes. */
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(key: TranslationKey, params?: Record<string, string | number>): string {
    return this.language.t(key, params);
  }
}

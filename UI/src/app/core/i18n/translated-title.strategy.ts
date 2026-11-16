import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { AppConfigService } from '../services/app-config.service';
import { LanguageService } from '../services/language.service';
import { TranslationKey } from './translations';

/** Route titles are translation keys: "Dashboard | Company name". */
@Injectable({ providedIn: 'root' })
export class TranslatedTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly language = inject(LanguageService);
  private readonly config = inject(AppConfigService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const key = this.buildTitle(snapshot) as TranslationKey | undefined;
    const company = this.config.companyName();
    this.title.setTitle(key ? `${this.language.t(key)} | ${company}` : company);
  }
}

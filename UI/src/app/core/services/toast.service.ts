import { Injectable, inject } from '@angular/core';
import { IndividualConfig, ToastrService } from 'ngx-toastr';
import { LanguageService } from './language.service';

/**
 * App messages as Toastr notifications (they appear, then go away by themselves).
 * Top corner on the reading side: right in English, left in Arabic.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastr = inject(ToastrService);
  private readonly language = inject(LanguageService);

  success(text: string, title?: string): void {
    this.toastr.success(text, title, this.options(4000));
  }

  error(text: string, title?: string): void {
    this.toastr.error(text, title, this.options(6500));
  }

  info(text: string, title?: string): void {
    this.toastr.info(text, title, this.options(5000));
  }

  warning(text: string, title?: string): void {
    this.toastr.warning(text, title, this.options(6000));
  }

  private options(timeOut: number): Partial<IndividualConfig> {
    return {
      timeOut,
      positionClass: this.language.isArabic() ? 'toast-top-left' : 'toast-top-right'
    };
  }
}

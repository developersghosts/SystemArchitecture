import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LanguageService } from '../../../core/services/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DialogComponent } from '../../../shared/dialog/dialog.component';
import { IconComponent } from '../../../shared/icon/icon.component';

interface MessageText {
  key: string;
  en: string | null;
  egyptian: string | null;
  saudi: string | null;
  modifiedAtUtc: string | null;
}

interface MessageDictionary {
  country: string | null;
  arabicDialect: 'ar-EG' | 'ar-SA' | null;
  items: MessageText[];
}

const MAX = 1000;

/** The message dictionary saved in the database (GET/PUT /api/Configuration/messages), Administrator only. */
@Component({
  selector: 'app-message-dictionary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, DialogComponent, IconComponent, TranslatePipe],
  templateUrl: './message-dictionary.component.html'
})
export class MessageDictionaryComponent {
  private readonly http = inject(HttpClient);
  private readonly language = inject(LanguageService);
  private readonly toast = inject(ToastService);
  private readonly api = `${environment.apiUrl}/api/Configuration/messages`;

  protected readonly data = signal<MessageDictionary | null>(null);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly dialect = computed(() => this.data()?.arabicDialect ?? 'standard');

  protected readonly editing = signal<MessageText | null>(null);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    en: ['', [Validators.required, Validators.maxLength(MAX)]],
    egyptian: ['', [Validators.required, Validators.maxLength(MAX)]],
    saudi: ['', [Validators.required, Validators.maxLength(MAX)]]
  });

  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.load();
    inject(DestroyRef).onDestroy(() => clearTimeout(this.searchTimer));
  }

  protected load(): void {
    this.loading.set(true);
    const params = this.search().trim() ? new HttpParams().set('search', this.search().trim()) : undefined;
    this.http.get<MessageDictionary>(this.api, { params }).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.load(), 350);
  }

  protected edit(item: MessageText): void {
    this.form.reset({ en: item.en ?? '', egyptian: item.egyptian ?? '', saudi: item.saudi ?? '' });
    this.editing.set(item);
  }

  protected invalid(name: 'en' | 'egyptian' | 'saudi'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  protected save(): void {
    const item = this.editing();
    if (!item) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.http.put<MessageText>(this.api, { key: item.key, ...this.form.getRawValue() }).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.editing.set(null);
        this.data.update((d) => (d ? { ...d, items: d.items.map((i) => (i.key === saved.key ? saved : i)) } : d));
        this.toast.success(this.language.t('messages.saved'));
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, this.language));
      }
    });
  }
}

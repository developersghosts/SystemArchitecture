import { Routes } from '@angular/router';
import { AccountComponent } from './account.component';

/** Lazy loaded at /account (any signed-in user). */
export const ACCOUNT_ROUTES: Routes = [{ path: '', component: AccountComponent, title: 'account.title' }];

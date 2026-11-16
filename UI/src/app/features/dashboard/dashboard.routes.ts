import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';

/** Lazy loaded at /dashboard. */
export const DASHBOARD_ROUTES: Routes = [{ path: '', component: DashboardComponent, title: 'nav.dashboard' }];

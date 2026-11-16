import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards/auth.guards';
import { Roles } from './core/models/auth.models';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

/**
 * Every feature is lazy loaded (its own JS chunk, downloaded on first visit).
 * canMatch guards run before the chunk is downloaded.
 */
export const routes: Routes = [
  {
    path: 'auth',
    canMatch: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canMatch: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES)
      },
      {
        path: 'users',
        canMatch: [roleGuard(Roles.Administrator, Roles.GetAllUsers)],
        loadChildren: () => import('./features/users/users.routes').then((m) => m.USERS_ROUTES)
      },
      {
        path: 'roles',
        canMatch: [roleGuard(Roles.Administrator, Roles.GetAllRolesList)],
        loadChildren: () => import('./features/roles/roles.routes').then((m) => m.ROLES_ROUTES)
      },
      {
        path: 'groups',
        canMatch: [roleGuard(Roles.Administrator, Roles.GetAllRolesList)],
        loadChildren: () => import('./features/groups/groups.routes').then((m) => m.GROUPS_ROUTES)
      },
      {
        path: 'account',
        loadChildren: () => import('./features/account/account.routes').then((m) => m.ACCOUNT_ROUTES)
      },
      {
        path: 'settings',
        canMatch: [roleGuard(Roles.Administrator)],
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];

import { Routes } from '@angular/router';
import { UsersListComponent } from './users-list/users-list.component';

/** Lazy loaded at /users (needs Administrator or GetAllUsers, see app.routes.ts). */
export const USERS_ROUTES: Routes = [{ path: '', component: UsersListComponent, title: 'nav.users' }];

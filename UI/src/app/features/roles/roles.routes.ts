import { Routes } from '@angular/router';
import { RolesListComponent } from './roles-list.component';

/** Lazy loaded at /roles (needs Administrator or GetAllRolesList, see app.routes.ts). */
export const ROLES_ROUTES: Routes = [{ path: '', component: RolesListComponent, title: 'nav.roles' }];

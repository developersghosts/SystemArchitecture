import { Routes } from '@angular/router';
import { GroupsListComponent } from './groups-list.component';

/** Lazy loaded at /groups (Administrator or GetAllRolesList, see app.routes.ts). */
export const GROUPS_ROUTES: Routes = [{ path: '', component: GroupsListComponent, title: 'nav.groups' }];

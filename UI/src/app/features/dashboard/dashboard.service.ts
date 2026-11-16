import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse, RoleListItem, UserListItem } from '../../core/models/auth.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  users(pageSize = 5): Observable<PagedResponse<UserListItem>> {
    const params = new HttpParams().set('pageNumber', 1).set('pageSize', pageSize);
    return this.http.get<PagedResponse<UserListItem>>(`${this.api}/api/Account/getAllUserList`, { params });
  }

  roles(): Observable<PagedResponse<RoleListItem>> {
    const params = new HttpParams().set('pageNumber', 1).set('pageSize', 1);
    return this.http.get<PagedResponse<RoleListItem>>(`${this.api}/api/Roles/getAllRolesList`, { params });
  }
}

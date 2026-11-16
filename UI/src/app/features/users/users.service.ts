import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateUserRequest,
  EditUserRequest,
  PagedResponse,
  RoleListItem,
  UserListItem,
  UserRole
} from '../../core/models/auth.models';

/** Calls the ServicesA user endpoints (Account, Roles, UserRoles). */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  list(page: number, pageSize: number, search: string): Observable<PagedResponse<UserListItem>> {
    let params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('findValues', search.trim());
    return this.http.get<PagedResponse<UserListItem>>(`${this.api}/api/Account/getAllUserList`, { params });
  }

  /** With roles -> registerUserFull (Administrator); without -> register (RegisterUser role is enough). */
  create(request: CreateUserRequest, withRoles: boolean): Observable<string> {
    return withRoles
      ? this.http.post<string>(`${this.api}/api/Account/registerUserFull`, request)
      : this.http.post<string>(`${this.api}/api/Account/register`, { ...request, roleIds: undefined });
  }

  edit(request: EditUserRequest): Observable<string> {
    return this.http.post<string>(`${this.api}/api/Account/editUser`, request);
  }

  allRoles(): Observable<PagedResponse<RoleListItem>> {
    const params = new HttpParams().set('pageNumber', 1).set('pageSize', 50);
    return this.http.get<PagedResponse<RoleListItem>>(`${this.api}/api/Roles/getAllRolesList`, { params });
  }

  userRoles(userId: string): Observable<UserRole[]> {
    return this.http.get<UserRole[]>(`${this.api}/api/UserRoles/getAllUserRolesList/${userId}`);
  }

  setRoles(userId: string, roleIds: string[]): Observable<UserRole[]> {
    return this.http.put<UserRole[]>(`${this.api}/api/UserRoles/setUserRoles`, { userId, roleIds });
  }
}

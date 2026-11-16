import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse, UserGroupDetails, UserGroupListItem } from '../../core/models/auth.models';

export interface GroupBody {
  name: string;
  description: string | null;
  roleIds: string[];
}

/** /api/UserGroups: groups of roles that can be given to users. */
@Injectable({ providedIn: 'root' })
export class GroupsService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/api/UserGroups`;

  list(page: number, pageSize: number, search: string): Observable<PagedResponse<UserGroupListItem>> {
    let params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('findValues', search.trim());
    return this.http.get<PagedResponse<UserGroupListItem>>(`${this.api}/getAllGroups`, { params });
  }

  /** Every group (the API caps a page at 50). */
  all(): Observable<PagedResponse<UserGroupListItem>> {
    return this.list(1, 50, '');
  }

  get(id: string): Observable<UserGroupDetails> {
    return this.http.get<UserGroupDetails>(`${this.api}/${id}`);
  }

  create(body: GroupBody): Observable<UserGroupDetails> {
    return this.http.post<UserGroupDetails>(`${this.api}/createGroup`, body);
  }

  update(id: string, body: GroupBody): Observable<UserGroupDetails> {
    return this.http.put<UserGroupDetails>(`${this.api}/updateGroup`, { id, ...body });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }

  ofUser(userId: string): Observable<UserGroupListItem[]> {
    return this.http.get<UserGroupListItem[]>(`${this.api}/user/${userId}`);
  }

  setUserGroups(userId: string, groupIds: string[]): Observable<UserGroupListItem[]> {
    return this.http.put<UserGroupListItem[]>(`${this.api}/setUserGroups`, { userId, groupIds });
  }
}

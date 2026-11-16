import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse, RoleListItem } from '../../core/models/auth.models';

export interface CreateRoleItem {
  name: string;
  title: string;
  description: string | null;
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/api/Roles`;

  list(page: number, pageSize: number, search: string): Observable<PagedResponse<RoleListItem>> {
    let params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('findValues', search.trim());
    return this.http.get<PagedResponse<RoleListItem>>(`${this.api}/getAllRolesList`, { params });
  }

  /** The API takes a list and skips names that already exist; returns the titles it created. */
  create(roles: CreateRoleItem[]): Observable<string[]> {
    return this.http.post<string[]>(`${this.api}/createRole`, { roles });
  }
}

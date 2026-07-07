import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7180/api/users';

  getUsers(page: number = 1, pageSize: number = 10, search: string = ''): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<any>(this.baseUrl, { params });
  }

  getUser(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  updateUser(id: string, data: { fullName: string; avatarUrl?: string | null }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, data);
  }

  updateUserStatus(id: string, status: 'Active' | 'Inactive'): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/status`, { status });
  }
}

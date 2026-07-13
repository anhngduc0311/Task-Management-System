import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductCategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/product-categories';

  getTree(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  getChildren(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${id}/children`);
  }

  create(data: { parentId?: string | null; code: string; name: string; description?: string | null; isActive: boolean; displayOrder?: number }): Observable<any> {
    return this.http.post<any>(this.baseUrl, data);
  }

  update(id: string, data: { parentId?: string | null; code: string; name: string; description?: string | null; isActive: boolean; displayOrder?: number }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}`);
  }
}

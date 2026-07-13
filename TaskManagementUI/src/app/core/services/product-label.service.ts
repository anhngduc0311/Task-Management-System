import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductLabelService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/product-labels';

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  create(data: { code: string; name: string; color: string; isActive: boolean }): Observable<any> {
    return this.http.post<any>(this.baseUrl, data);
  }

  update(id: string, data: { code: string; name: string; color: string; isActive: boolean }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}`);
  }
}

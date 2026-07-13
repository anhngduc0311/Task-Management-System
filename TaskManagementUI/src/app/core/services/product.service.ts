import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/products';

  getProducts(page: number = 1, pageSize: number = 10, search?: string, categoryId?: string, status?: string): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (categoryId) params = params.set('categoryId', categoryId);
    if (status) params = params.set('status', status);

    return this.http.get<any>(this.baseUrl, { params });
  }

  searchProducts(searchRequest: {
    search?: string;
    categoryId?: string;
    includeChildCategories?: boolean;
    status?: string;
    originId?: string;
    supplierId?: string;
    labelId?: string;
    minPrice?: number;
    maxPrice?: number;
    sortBy?: string;
    sortDescending?: boolean;
    page: number;
    pageSize: number;
  }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/search`, searchRequest);
  }

  getProduct(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  createProduct(data: any): Observable<any> {
    return this.http.post<any>(this.baseUrl, data);
  }

  updateProduct(id: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, data);
  }

  deleteProduct(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}`);
  }

  // Images
  uploadImages(id: string, files: File[]): Observable<any> {
    const formData = new FormData();
    files.forEach(file => {
      formData.append('files', file, file.name);
    });
    return this.http.post<any>(`${this.baseUrl}/${id}/images`, formData);
  }

  deleteImage(id: string, imageId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}/images/${imageId}`);
  }

  setPrimaryImage(id: string, imageId: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}/images/${imageId}/primary`, {});
  }

  // Variants
  getVariants(productId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${productId}/variants`);
  }

  createVariant(productId: string, data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${productId}/variants`, data);
  }

  updateVariant(productId: string, variantId: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${productId}/variants/${variantId}`, data);
  }

  deleteVariant(productId: string, variantId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${productId}/variants/${variantId}`);
  }

  // Unit Conversions
  getConversions(productId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${productId}/unit-conversions`);
  }

  createConversion(productId: string, data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${productId}/unit-conversions`, data);
  }

  updateConversion(productId: string, conversionId: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${productId}/unit-conversions/${conversionId}`, data);
  }

  deleteConversion(productId: string, conversionId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${productId}/unit-conversions/${conversionId}`);
  }

  // Attribute Groups
  getAttributeGroups(productId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${productId}/attribute-groups`);
  }

  createAttributeGroup(productId: string, data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${productId}/attribute-groups`, data);
  }

  updateAttributeGroup(productId: string, groupId: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${productId}/attribute-groups/${groupId}`, data);
  }

  deleteAttributeGroup(productId: string, groupId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${productId}/attribute-groups/${groupId}`);
  }
}

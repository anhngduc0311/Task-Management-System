import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/inventory';

  getStockBalances(page: number = 1, pageSize: number = 10, warehouseId?: string, productId?: string, productVariantId?: string): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (productId) params = params.set('productId', productId);
    if (productVariantId) params = params.set('productVariantId', productVariantId);

    return this.http.get<any>(`${this.baseUrl}/stock-balances`, { params });
  }

  getStockMovements(page: number = 1, pageSize: number = 10, warehouseId?: string, productId?: string, movementType?: number, startDate?: string, endDate?: string): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (productId) params = params.set('productId', productId);
    if (movementType !== undefined && movementType !== null) params = params.set('movementType', movementType.toString());
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get<any>(`${this.baseUrl}/stock-movements`, { params });
  }
}

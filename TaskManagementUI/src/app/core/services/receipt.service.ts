import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/inventory';

  // Import Receipts
  getImportReceipts(page: number = 1, pageSize: number = 10, warehouseId?: string, supplierId?: string, status?: number): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (supplierId) params = params.set('supplierId', supplierId);
    if (status !== undefined && status !== null) params = params.set('status', status.toString());

    return this.http.get<any>(`${this.baseUrl}/import-receipts`, { params });
  }

  getImportReceipt(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/import-receipts/${id}`);
  }

  createImportReceipt(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/import-receipts`, data);
  }

  updateImportReceipt(id: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/import-receipts/${id}`, data);
  }

  confirmImportReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/import-receipts/${id}/confirm`, {});
  }

  cancelImportReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/import-receipts/${id}/cancel`, {});
  }

  // Export Receipts
  getExportReceipts(page: number = 1, pageSize: number = 10, warehouseId?: string, status?: number): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (status !== undefined && status !== null) params = params.set('status', status.toString());

    return this.http.get<any>(`${this.baseUrl}/export-receipts`, { params });
  }

  getExportReceipt(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/export-receipts/${id}`);
  }

  createExportReceipt(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/export-receipts`, data);
  }

  updateExportReceipt(id: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/export-receipts/${id}`, data);
  }

  confirmExportReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/export-receipts/${id}/confirm`, {});
  }

  cancelExportReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/export-receipts/${id}/cancel`, {});
  }

  // Transfer Receipts
  getTransferReceipts(page: number = 1, pageSize: number = 10, fromWarehouseId?: string, toWarehouseId?: string, status?: number): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (fromWarehouseId) params = params.set('fromWarehouseId', fromWarehouseId);
    if (toWarehouseId) params = params.set('toWarehouseId', toWarehouseId);
    if (status !== undefined && status !== null) params = params.set('status', status.toString());

    return this.http.get<any>(`${this.baseUrl}/transfer-receipts`, { params });
  }

  getTransferReceipt(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/transfer-receipts/${id}`);
  }

  createTransferReceipt(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/transfer-receipts`, data);
  }

  updateTransferReceipt(id: string, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/transfer-receipts/${id}`, data);
  }

  confirmTransferReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/transfer-receipts/${id}/confirm`, {});
  }

  cancelTransferReceipt(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/transfer-receipts/${id}/cancel`, {});
  }
}

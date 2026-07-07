import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DynamicFieldService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api';

  getProjectDynamicFields(projectId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/projects/${projectId}/dynamic-fields`);
  }

  createDynamicField(projectId: string, data: { fieldName: string; fieldKey: string; fieldType: string; isRequired: boolean; options?: string[] | null; defaultValue?: string | null; displayOrder: number }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/projects/${projectId}/dynamic-fields`, data);
  }

  updateDynamicField(fieldId: string, data: { fieldName: string; isRequired: boolean; options?: string[] | null; defaultValue?: string | null; displayOrder: number; isActive: boolean }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/dynamic-fields/${fieldId}`, data);
  }

  deleteDynamicField(fieldId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/dynamic-fields/${fieldId}`);
  }

  getTaskDynamicValues(taskId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/tasks/${taskId}/dynamic-values`);
  }

  updateTaskDynamicValues(taskId: string, values: Record<string, string>): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/tasks/${taskId}/dynamic-values`, values);
  }
}

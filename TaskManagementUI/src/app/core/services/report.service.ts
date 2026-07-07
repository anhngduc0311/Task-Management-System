import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/reports';

  private buildParams(filter: any, page?: number, pageSize?: number): HttpParams {
    let params = new HttpParams();

    if (filter) {
      if (filter.projectId) params = params.set('projectId', filter.projectId);
      if (filter.assigneeId) params = params.set('assigneeId', filter.assigneeId);
      if (filter.status) params = params.set('status', filter.status);
      if (filter.priority) params = params.set('priority', filter.priority);
      if (filter.dateFrom) params = params.set('dateFrom', new Date(filter.dateFrom).toISOString());
      if (filter.dateTo) params = params.set('dateTo', new Date(filter.dateTo).toISOString());
      if (filter.createdAtFrom) params = params.set('createdAtFrom', new Date(filter.createdAtFrom).toISOString());
      if (filter.createdAtTo) params = params.set('createdAtTo', new Date(filter.createdAtTo).toISOString());
      if (filter.dueDateFrom) params = params.set('dueDateFrom', new Date(filter.dueDateFrom).toISOString());
      if (filter.dueDateTo) params = params.set('dueDateTo', new Date(filter.dueDateTo).toISOString());
      if (filter.completedAtFrom) params = params.set('completedAtFrom', new Date(filter.completedAtFrom).toISOString());
      if (filter.completedAtTo) params = params.set('completedAtTo', new Date(filter.completedAtTo).toISOString());

      if (filter.dynamicFields) {
        Object.keys(filter.dynamicFields).forEach(key => {
          if (filter.dynamicFields[key]) {
            params = params.set(`DynamicFields[${key}]`, filter.dynamicFields[key]);
          }
        });
      }
    }

    if (page !== undefined) params = params.set('page', page.toString());
    if (pageSize !== undefined) params = params.set('pageSize', pageSize.toString());

    return params;
  }

  getWorkSummary(filter: any): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/work-summary`, { params: this.buildParams(filter) });
  }

  getTasksByStatus(filter: any): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/tasks-by-status`, { params: this.buildParams(filter) });
  }

  getTasksByPriority(filter: any): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/tasks-by-priority`, { params: this.buildParams(filter) });
  }

  getTasksByAssignee(filter: any): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/tasks-by-assignee`, { params: this.buildParams(filter) });
  }

  getTasksByProject(filter: any): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/tasks-by-project`, { params: this.buildParams(filter) });
  }

  getOverdueTasks(filter: any, page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/overdue-tasks`, { params: this.buildParams(filter, page, pageSize) });
  }

  getCompletedTasks(filter: any, page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/completed-tasks`, { params: this.buildParams(filter, page, pageSize) });
  }

  getUncompletedTasks(filter: any, page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/uncompleted-tasks`, { params: this.buildParams(filter, page, pageSize) });
  }

  getAdvancedTasks(advancedFilter: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/advanced`, advancedFilter);
  }
}

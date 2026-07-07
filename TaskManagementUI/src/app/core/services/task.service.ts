import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7180/api';

  getProjectTasks(projectId: string, filters: any = {}): Observable<any> {
    let params = new HttpParams();
    if (filters.search) params = params.set('search', filters.search);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.priority) params = params.set('priority', filters.priority);
    if (filters.assigneeId) params = params.set('assigneeId', filters.assigneeId);
    if (filters.dueDate) params = params.set('dueDate', filters.dueDate);
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<any>(`${this.baseUrl}/projects/${projectId}/tasks`, { params });
  }

  createTask(projectId: string, data: { title: string; description?: string | null; priority: string; assigneeId?: string | null; dueDate?: string | null }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/projects/${projectId}/tasks`, data);
  }

  getTask(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/tasks/${id}`);
  }

  updateTask(id: string, data: { title: string; description?: string | null; status: string; priority: string; assigneeId?: string | null; dueDate?: string | null; rowVersion: string }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/tasks/${id}`, data);
  }

  updateTaskStatus(id: string, status: string): Observable<any> {
    return this.http.patch<any>(`${this.baseUrl}/tasks/${id}/status`, { status });
  }

  updateTaskAssignee(id: string, assigneeId: string | null): Observable<any> {
    return this.http.patch<any>(`${this.baseUrl}/tasks/${id}/assignee`, { assigneeId });
  }

  deleteTask(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/tasks/${id}`);
  }

  getMyTasks(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/tasks/my-tasks`);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7180/api';

  getProjectAuditLogs(projectId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/projects/${projectId}/audit-logs`);
  }

  getTaskAuditLogs(taskId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/tasks/${taskId}/audit-logs`);
  }
}

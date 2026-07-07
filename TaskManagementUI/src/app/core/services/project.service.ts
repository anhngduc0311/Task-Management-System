import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/projects';

  getProjects(page: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(this.baseUrl, { params });
  }

  createProject(data: { name: string; description?: string | null }): Observable<any> {
    return this.http.post<any>(this.baseUrl, data);
  }

  getProject(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  updateProject(id: string, data: { name: string; description?: string | null; status: string }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, data);
  }

  deleteProject(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}`);
  }

  // Project Members
  getMembers(projectId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${projectId}/members`);
  }

  addMember(projectId: string, data: { email: string; roleInProject: string }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${projectId}/members`, data);
  }

  updateMemberRole(projectId: string, userId: string, roleInProject: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${projectId}/members/${userId}`, { roleInProject });
  }

  removeMember(projectId: string, userId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${projectId}/members/${userId}`);
  }
}

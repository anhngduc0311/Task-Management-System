import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/tasks';

  getComments(taskId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${taskId}/comments`);
  }

  createComment(taskId: string, content: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${taskId}/comments`, { content });
  }

  updateComment(taskId: string, commentId: string, content: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${taskId}/comments/${commentId}`, { content });
  }

  deleteComment(taskId: string, commentId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${taskId}/comments/${commentId}`);
  }
}

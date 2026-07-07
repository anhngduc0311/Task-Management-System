import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AttachmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7180/api';

  getAttachments(taskId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/tasks/${taskId}/attachments`);
  }

  uploadAttachment(taskId: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<any>(`${this.baseUrl}/tasks/${taskId}/attachments`, formData);
  }

  downloadAttachment(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/attachments/${id}/download`, { responseType: 'blob' });
  }

  deleteAttachment(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/attachments/${id}`);
  }
}

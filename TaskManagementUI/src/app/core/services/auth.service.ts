import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5035/api/auth';

  readonly currentUser = signal<any>(this.getStoredUser());

  login(request: any) {
    return this.http.post<any>(`${this.baseUrl}/login`, request).pipe(
      tap(res => {
        localStorage.setItem('accessToken', res.accessToken);
        localStorage.setItem('refreshToken', res.refreshToken);
        const user = {
          id: res.userId,
          fullName: res.fullName,
          email: res.email,
          roles: res.roles || []
        };
        localStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUser.set(user);
      })
    );
  }

  logout() {
    // Attempt logout on server, but finalize by clearing local state
    return this.http.post(`${this.baseUrl}/logout`, {}).pipe(
      tap({
        finalize: () => {
          this.clearLocalSession();
        }
      })
    );
  }

  clearLocalSession() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    this.currentUser.set(null);
  }

  changePassword(request: any) {
    return this.http.post(`${this.baseUrl}/change-password`, request);
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('accessToken');
  }

  private getStoredUser() {
    const userStr = localStorage.getItem('currentUser');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch (e) {
        return null;
      }
    }
    return null;
  }
}

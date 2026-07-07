import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-overlay">
      <div class="gradient-blob blob-1"></div>
      <div class="gradient-blob blob-2"></div>

      <div class="glass-card login-card animate-scale-up">
        <div class="card-header">
          <span class="material-symbols-rounded logo-icon">blur_on</span>
          <h2>Welcome back</h2>
          <p>Login to manage your tasks</p>
        </div>

        <form #loginForm="ngForm" (ngSubmit)="onSubmit(loginForm)" class="login-form">
          <div class="form-group">
            <label class="form-label" for="email">Email Address</label>
            <input 
              type="email" 
              id="email" 
              name="email" 
              [(ngModel)]="credentials.email" 
              #email="ngModel" 
              required 
              email
              class="form-input" 
              placeholder="name@company.com" 
              [disabled]="loading()"
            />
            @if (email.invalid && (email.dirty || email.touched)) {
              <span class="error-text">Please enter a valid email address.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="password">Password</label>
            <input 
              type="password" 
              id="password" 
              name="password" 
              [(ngModel)]="credentials.password" 
              #password="ngModel" 
              required 
              class="form-input" 
              placeholder="••••••••" 
              [disabled]="loading()"
            />
            @if (password.invalid && (password.dirty || password.touched)) {
              <span class="error-text">Password is required.</span>
            }
          </div>

          <button 
            type="submit" 
            class="btn btn-primary w-full mt-8" 
            [disabled]="loginForm.invalid || loading()"
          >
            @if (loading()) {
              Logging in...
            } @else {
              Sign In
            }
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .login-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: #f1f5f9;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
    }
    
    .gradient-blob {
      position: absolute;
      width: 400px;
      height: 400px;
      border-radius: 50%;
      filter: blur(100px);
      opacity: 0.15;
      z-index: 0;
    }
    .blob-1 {
      top: -100px;
      left: -100px;
      background-color: var(--primary);
    }
    .blob-2 {
      bottom: -100px;
      right: -100px;
      background-color: var(--secondary);
    }

    .login-card {
      width: 90%;
      max-width: 420px;
      position: relative;
      z-index: 1;
      padding: 40px 32px;
      background: rgba(255, 255, 255, 0.85);
      border: 1px solid rgba(255, 255, 255, 0.4);
      box-shadow: 0 25px 50px -12px rgba(15, 23, 42, 0.08);
    }

    .card-header {
      margin-bottom: 32px;
      text-align: center;
    }
    .logo-icon {
      font-size: 48px;
      color: var(--primary);
      margin-bottom: 12px;
    }
    .card-header h2 {
      font-size: 1.75rem;
      font-weight: 700;
      margin-bottom: 6px;
    }
    .card-header p {
      color: var(--text-muted);
      font-size: 0.9rem;
    }

    .login-form {
      display: flex;
      flex-direction: column;
    }

    .error-text {
      font-size: 0.75rem;
      color: var(--danger);
      margin-top: 4px;
      display: block;
    }
  `]
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected credentials = {
    email: '',
    password: ''
  };
  
  protected loading = signal(false);

  onSubmit(form: any) {
    if (form.invalid) return;

    this.loading.set(true);
    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.toastService.success('Logged in successfully.');
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        const errMsg = err.error?.message || 'Invalid email or password.';
        this.toastService.error(errMsg);
      }
    });
  }
}

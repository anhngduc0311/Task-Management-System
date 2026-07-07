import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="login-overlay">
      <div class="gradient-blob blob-1"></div>
      <div class="gradient-blob blob-2"></div>

      <div class="glass-card login-card animate-scale-up">
        <div class="card-header">
          <span class="material-symbols-rounded logo-icon">blur_on</span>
          <h2>Create an account</h2>
          <p>Get started with Taskly today</p>
        </div>

        <form #registerForm="ngForm" (ngSubmit)="onSubmit(registerForm)" class="login-form">
          <div class="form-group">
            <label class="form-label" for="fullName">Full Name</label>
            <input 
              type="text" 
              id="fullName" 
              name="fullName" 
              [(ngModel)]="user.fullName" 
              #fullName="ngModel" 
              required 
              minlength="2"
              class="form-input" 
              placeholder="John Doe" 
              [disabled]="loading()"
            />
            @if (fullName.invalid && (fullName.dirty || fullName.touched)) {
              <span class="error-text">Please enter your full name (at least 2 characters).</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="email">Email Address</label>
            <input 
              type="email" 
              id="email" 
              name="email" 
              [(ngModel)]="user.email" 
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
              [(ngModel)]="user.password" 
              #password="ngModel" 
              required 
              minlength="6"
              class="form-input" 
              placeholder="••••••••" 
              [disabled]="loading()"
            />
            @if (password.invalid && (password.dirty || password.touched)) {
              <span class="error-text">Password must be at least 6 characters long.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="confirmPassword">Confirm Password</label>
            <input 
              type="password" 
              id="confirmPassword" 
              name="confirmPassword" 
              [(ngModel)]="user.confirmPassword" 
              #confirmPassword="ngModel" 
              required 
              class="form-input" 
              placeholder="••••••••" 
              [disabled]="loading()"
            />
            @if (confirmPassword.touched && user.password !== user.confirmPassword) {
              <span class="error-text">Passwords do not match.</span>
            }
          </div>

          <button 
            type="submit" 
            class="btn btn-primary w-full mt-8" 
            [disabled]="registerForm.invalid || user.password !== user.confirmPassword || loading()"
          >
            @if (loading()) {
              Creating account...
            } @else {
              Sign Up
            }
          </button>

          <p class="mt-16 text-center text-sm" style="color: var(--text-muted); margin-top: 16px; text-align: center;">
            Already have an account? <a routerLink="/login" style="color: var(--primary); font-weight: 500; text-decoration: none;">Sign In</a>
          </p>
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
      padding: 32px 28px;
      background: rgba(255, 255, 255, 0.85);
      border: 1px solid rgba(255, 255, 255, 0.4);
      box-shadow: 0 25px 50px -12px rgba(15, 23, 42, 0.08);
      border-radius: 16px;
    }

    .card-header {
      margin-bottom: 24px;
      text-align: center;
    }
    .logo-icon {
      font-size: 44px;
      color: var(--primary);
      margin-bottom: 8px;
    }
    .card-header h2 {
      font-size: 1.5rem;
      font-weight: 700;
      margin-bottom: 4px;
    }
    .card-header p {
      color: var(--text-muted);
      font-size: 0.85rem;
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .form-label {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .form-input {
      padding: 10px 14px;
      border: 1px solid var(--border);
      border-radius: 8px;
      font-size: 0.9rem;
      transition: all var(--transition-fast);
      background-color: white;
    }
    .form-input:focus {
      outline: none;
      border-color: var(--primary);
      box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);
    }

    .btn-primary {
      padding: 10px;
      background-color: var(--primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color var(--transition-fast);
    }
    .btn-primary:hover {
      background-color: var(--primary-hover);
    }
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .error-text {
      font-size: 0.75rem;
      color: var(--danger);
      margin-top: 2px;
      display: block;
    }
  `]
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected user = {
    fullName: '',
    email: '',
    password: '',
    confirmPassword: ''
  };
  
  protected loading = signal(false);

  onSubmit(form: any) {
    if (form.invalid || this.user.password !== this.user.confirmPassword) return;

    this.loading.set(true);
    const { fullName, email, password } = this.user;
    this.authService.register({ fullName, email, password }).subscribe({
      next: () => {
        this.toastService.success('Registration successful! Please login.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.loading.set(false);
        const errMsg = err.error?.message || 'Registration failed. Email might already be taken.';
        this.toastService.error(errMsg);
      }
    });
  }
}

import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-wrapper">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast-card" [ngClass]="'toast-' + toast.type" (click)="toastService.remove(toast.id)">
          <span class="material-symbols-rounded icon">
            {{ getIcon(toast.type) }}
          </span>
          <span class="message">{{ toast.message }}</span>
          <button class="close-btn">&times;</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-wrapper {
      position: fixed;
      top: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 12px;
      max-width: 380px;
      width: calc(100% - 48px);
    }
    .toast-card {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 18px;
      background: rgba(255, 255, 255, 0.95);
      backdrop-filter: blur(8px);
      border: 1px solid rgba(0, 0, 0, 0.05);
      border-radius: 12px;
      box-shadow: 0 10px 15px -3px rgba(0,0,0,0.05), 0 4px 6px -4px rgba(0,0,0,0.05);
      color: var(--text-main);
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      user-select: none;
      animation: slideIn var(--transition-normal) forwards;
      position: relative;
    }
    .toast-card:hover {
      transform: translateY(-1px);
      box-shadow: 0 12px 20px -3px rgba(0,0,0,0.08);
    }
    .icon {
      font-size: 22px;
      flex-shrink: 0;
    }
    .message {
      flex-grow: 1;
      padding-right: 8px;
      line-height: 1.4;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.25rem;
      color: var(--text-light);
      cursor: pointer;
      line-height: 1;
      flex-shrink: 0;
    }
    .close-btn:hover {
      color: var(--text-muted);
    }
    .toast-success {
      border-left: 4px solid var(--success);
    }
    .toast-success .icon { color: var(--success); }
    .toast-error {
      border-left: 4px solid var(--danger);
    }
    .toast-error .icon { color: var(--danger); }
    .toast-warning {
      border-left: 4px solid var(--warning);
    }
    .toast-warning .icon { color: var(--warning); }
    .toast-info {
      border-left: 4px solid var(--info);
    }
    .toast-info .icon { color: var(--info); }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateX(30px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }
  `]
})
export class ToastContainer {
  protected readonly toastService = inject(ToastService);

  getIcon(type: string): string {
    switch (type) {
      case 'success': return 'check_circle';
      case 'error': return 'error';
      case 'warning': return 'warning';
      default: return 'info';
    }
  }
}

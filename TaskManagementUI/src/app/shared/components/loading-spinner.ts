import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (active) {
      <div class="spinner-overlay">
        <div class="spinner-container">
          <div class="double-bounce1"></div>
          <div class="double-bounce2"></div>
        </div>
      </div>
    }
  `,
  styles: [`
    .spinner-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(255, 255, 255, 0.75);
      backdrop-filter: blur(4px);
      z-index: 9999;
      display: flex;
      align-items: center;
      justify-content: center;
      animation: fadeIn var(--transition-fast) forwards;
    }
    .spinner-container {
      width: 48px;
      height: 48px;
      position: relative;
    }
    .double-bounce1, .double-bounce2 {
      width: 100%;
      height: 100%;
      border-radius: 50%;
      background-color: var(--primary);
      opacity: 0.6;
      position: absolute;
      top: 0;
      left: 0;
      animation: bounce 2.0s infinite ease-in-out;
    }
    .double-bounce2 {
      animation-delay: -1.0s;
      background-color: var(--secondary);
    }
    @keyframes bounce {
      0%, 100% {
        transform: scale(0.0);
      } 50% {
        transform: scale(1.0);
      }
    }
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `]
})
export class LoadingSpinner {
  @Input() active: boolean = false;
}

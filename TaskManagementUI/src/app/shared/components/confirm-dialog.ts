import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (active) {
      <div class="modal-overlay" (click)="onCancel()">
        <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>{{ title }}</h3>
            <button class="close-btn" (click)="onCancel()">&times;</button>
          </div>
          <div class="modal-body">
            <p>{{ message }}</p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="onCancel()">Cancel</button>
            <button class="btn btn-danger" (click)="onConfirm()">Confirm</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: var(--text-light);
      line-height: 1;
    }
    .close-btn:hover {
      color: var(--text-muted);
    }
    p {
      color: var(--text-muted);
      font-size: 0.95rem;
      line-height: 1.5;
    }
  `]
})
export class ConfirmDialog {
  @Input() active: boolean = false;
  @Input() title: string = 'Confirm Action';
  @Input() message: string = 'Are you sure you want to perform this action?';

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm() {
    this.confirm.emit();
  }

  onCancel() {
    this.cancel.emit();
  }
}

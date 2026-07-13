import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductLabelService } from '../../core/services/product-label.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-labels',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="labels-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search labels..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Label
        </button>
      </div>

      <!-- Labels List/Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Label</th>
              <th>Code</th>
              <th>Name</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (filteredLabels().length === 0) {
              <tr>
                <td colspan="5" class="text-center text-muted py-24">No labels found.</td>
              </tr>
            } @else {
              @for (label of filteredLabels(); track label.id) {
                <tr class="animate-fade-in">
                  <td>
                    <span class="chip-label" [style.background-color]="label.color + '15'" [style.color]="label.color" [style.border-color]="label.color + '40'">
                      <span class="dot" [style.background-color]="label.color"></span>
                      {{ label.name }}
                    </span>
                  </td>
                  <td><strong>{{ label.code }}</strong></td>
                  <td>{{ label.name }}</td>
                  <td>
                    <span class="badge" [class.badge-status-todo]="!label.isActive" [class.badge-status-progress]="label.isActive" style="background-color: label.isActive ? 'var(--success-bg)' : 'var(--border)'; color: label.isActive ? 'var(--success)' : 'var(--text-muted)'">
                      {{ label.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text" (click)="openEditModal(label)" title="Edit">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text" (click)="onDelete(label.id)" title="Delete">
                      <span class="material-symbols-rounded text-danger">delete</span>
                    </button>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Add/Edit Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ isEditMode() ? 'Edit Label' : 'Create New Label' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #labelForm="ngForm" (ngSubmit)="onSubmit(labelForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="label-code">Label Code</label>
                  <input 
                    type="text" 
                    id="label-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentLabel.code" 
                    #labelCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. HOT, NEW, SALE"
                    [disabled]="isEditMode()"
                  />
                  @if (labelCode.invalid && (labelCode.dirty || labelCode.touched)) {
                    <span class="error-text">Label code is required (max 50 chars).</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="label-name">Label Name</label>
                  <input 
                    type="text" 
                    id="label-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentLabel.name" 
                    #labelName="ngModel" 
                    required 
                    maxlength="100"
                    placeholder="e.g. Best Seller, Clearance Sale"
                  />
                  @if (labelName.invalid && (labelName.dirty || labelName.touched)) {
                    <span class="error-text">Label name is required (max 100 chars).</span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="label-color">Label Color</label>
                  <div class="flex align-center gap-12">
                    <input 
                      type="color" 
                      id="label-color" 
                      name="color" 
                      class="color-picker-input"
                      [(ngModel)]="currentLabel.color" 
                    />
                    <span class="color-text-preview" style="font-family: monospace;">{{ currentLabel.color }}</span>
                    <span class="chip-label ml-12" [style.background-color]="currentLabel.color + '15'" [style.color]="currentLabel.color" [style.border-color]="currentLabel.color + '40'">
                      <span class="dot" [style.background-color]="currentLabel.color"></span>
                      {{ currentLabel.name || 'Preview' }}
                    </span>
                  </div>
                </div>

                <div class="form-group flex align-center gap-8 mt-16">
                  <input 
                    type="checkbox" 
                    id="label-active" 
                    name="isActive" 
                    [(ngModel)]="currentLabel.isActive"
                  />
                  <label for="label-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="labelForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Label' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .labels-container {
      width: 100%;
    }
    .search-input {
      min-width: 280px;
    }
    .table-card {
      padding: 0;
      overflow: hidden;
    }
    .overflow-x {
      overflow-x: auto;
    }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    .data-table th, .data-table td {
      padding: 16px 24px;
      border-bottom: 1px solid var(--border);
    }
    .data-table th {
      background-color: #fafbfc;
      font-weight: 600;
      color: var(--text-muted);
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .data-table tbody tr:hover {
      background-color: rgba(241, 245, 249, 0.5);
    }
    .chip-label {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
      border: 1px solid transparent;
    }
    .chip-label .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
    }
    .color-picker-input {
      width: 44px;
      height: 38px;
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 2px;
      cursor: pointer;
      background: white;
    }
    .color-text-preview {
      font-size: 0.9rem;
      color: var(--text-muted);
    }
    .text-right {
      text-align: right;
    }
    .actions-cell {
      display: flex;
      justify-content: flex-end;
      gap: 4px;
    }
    .text-primary {
      color: var(--primary);
    }
    .text-danger {
      color: var(--danger);
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: var(--text-light);
    }
    .close-btn:hover {
      color: var(--text-muted);
    }
    .error-text {
      font-size: 0.75rem;
      color: var(--danger);
      margin-top: 4px;
      display: block;
    }
    .mb-24 { margin-bottom: 24px; }
    .flex { display: flex; }
    .justify-between { justify-content: space-between; }
    .align-center { align-items: center; }
    .flex-wrap { flex-wrap: wrap; }
    .gap-16 { gap: 16px; }
    .gap-12 { gap: 12px; }
    .gap-8 { gap: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    .text-center { text-align: center; }
    .mb-0 { margin-bottom: 0; }
    .ml-12 { margin-left: 12px; }
  `]
})
export class Labels implements OnInit {
  private readonly labelService = inject(ProductLabelService);
  private readonly toastService = inject(ToastService);

  protected labels = signal<any[]>([]);
  protected filteredLabels = signal<any[]>([]);
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentLabel = {
    id: '',
    code: '',
    name: '',
    color: '#6366f1',
    isActive: true
  };

  ngOnInit() {
    this.loadLabels();
  }

  loadLabels() {
    this.loading.set(true);
    this.labelService.getAll().subscribe({
      next: (data) => {
        this.labels.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load labels.');
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredLabels.set(this.labels());
    } else {
      this.filteredLabels.set(
        this.labels().filter(l => 
          l.code.toLowerCase().includes(query) || 
          l.name.toLowerCase().includes(query)
        )
      );
    }
  }

  onSearchChange() {
    this.applyFilter();
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentLabel = {
      id: '',
      code: '',
      name: '',
      color: '#6366f1',
      isActive: true
    };
    this.showModal.set(true);
  }

  openEditModal(label: any) {
    this.isEditMode.set(true);
    this.currentLabel = { ...label };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.labelService.update(this.currentLabel.id, this.currentLabel).subscribe({
        next: () => {
          this.toastService.success('Label updated successfully.');
          this.closeModal();
          this.loadLabels();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update label.');
          this.submitting.set(false);
        }
      });
    } else {
      this.labelService.create(this.currentLabel).subscribe({
        next: () => {
          this.toastService.success('Label created successfully.');
          this.closeModal();
          this.loadLabels();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create label.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this label?')) {
      this.loading.set(true);
      this.labelService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Label deleted successfully.');
          this.loadLabels();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete label.');
          this.loading.set(false);
        }
      });
    }
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OriginService } from '../../core/services/origin.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-origins',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="origins-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search origins..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Origin
        </button>
      </div>

      <!-- Origins List/Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (filteredOrigins().length === 0) {
              <tr>
                <td colspan="4" class="text-center text-muted py-24">No origins found.</td>
              </tr>
            } @else {
              @for (origin of filteredOrigins(); track origin.id) {
                <tr class="animate-fade-in">
                  <td><strong>{{ origin.code }}</strong></td>
                  <td>{{ origin.name }}</td>
                  <td>
                    <span class="badge" [class.badge-status-todo]="!origin.isActive" [class.badge-status-progress]="origin.isActive" style="background-color: origin.isActive ? 'var(--success-bg)' : 'var(--border)'; color: origin.isActive ? 'var(--success)' : 'var(--text-muted)'">
                      {{ origin.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text" (click)="openEditModal(origin)" title="Edit">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text" (click)="onDelete(origin.id)" title="Delete">
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
              <h3>{{ isEditMode() ? 'Edit Origin' : 'Create New Origin' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #originForm="ngForm" (ngSubmit)="onSubmit(originForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="origin-code">Origin Code</label>
                  <input 
                    type="text" 
                    id="origin-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentOrigin.code" 
                    #originCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. VN, US, JP"
                    [disabled]="isEditMode()"
                  />
                  @if (originCode.invalid && (originCode.dirty || originCode.touched)) {
                    <span class="error-text">Origin code is required (max 50 chars).</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="origin-name">Origin Name</label>
                  <input 
                    type="text" 
                    id="origin-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentOrigin.name" 
                    #originName="ngModel" 
                    required 
                    maxlength="100"
                    placeholder="e.g. Vietnam, United States, Japan"
                  />
                  @if (originName.invalid && (originName.dirty || originName.touched)) {
                    <span class="error-text">Origin name is required (max 100 chars).</span>
                  }
                </div>

                <div class="form-group flex align-center gap-8 mt-16">
                  <input 
                    type="checkbox" 
                    id="origin-active" 
                    name="isActive" 
                    [(ngModel)]="currentOrigin.isActive"
                  />
                  <label for="origin-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="originForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Origin' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .origins-container {
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
    .gap-8 { gap: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    .text-center { text-align: center; }
    .mb-0 { margin-bottom: 0; }
  `]
})
export class Origins implements OnInit {
  private readonly originService = inject(OriginService);
  private readonly toastService = inject(ToastService);

  protected origins = signal<any[]>([]);
  protected filteredOrigins = signal<any[]>([]);
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentOrigin = {
    id: '',
    code: '',
    name: '',
    isActive: true
  };

  ngOnInit() {
    this.loadOrigins();
  }

  loadOrigins() {
    this.loading.set(true);
    this.originService.getAll().subscribe({
      next: (data) => {
        this.origins.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load origins.');
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredOrigins.set(this.origins());
    } else {
      this.filteredOrigins.set(
        this.origins().filter(o => 
          o.code.toLowerCase().includes(query) || 
          o.name.toLowerCase().includes(query)
        )
      );
    }
  }

  onSearchChange() {
    this.applyFilter();
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentOrigin = {
      id: '',
      code: '',
      name: '',
      isActive: true
    };
    this.showModal.set(true);
  }

  openEditModal(origin: any) {
    this.isEditMode.set(true);
    this.currentOrigin = { ...origin };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.originService.update(this.currentOrigin.id, this.currentOrigin).subscribe({
        next: () => {
          this.toastService.success('Origin updated successfully.');
          this.closeModal();
          this.loadOrigins();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update origin.');
          this.submitting.set(false);
        }
      });
    } else {
      this.originService.create(this.currentOrigin).subscribe({
        next: () => {
          this.toastService.success('Origin created successfully.');
          this.closeModal();
          this.loadOrigins();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create origin.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this origin?')) {
      this.loading.set(true);
      this.originService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Origin deleted successfully.');
          this.loadOrigins();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete origin.');
          this.loading.set(false);
        }
      });
    }
  }
}

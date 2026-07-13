import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WarehouseService } from '../../core/services/warehouse.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-warehouses',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="warehouses-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search warehouses..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Warehouse
        </button>
      </div>

      <!-- Warehouses List/Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Address</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (filteredWarehouses().length === 0) {
              <tr>
                <td colspan="5" class="text-center text-muted py-24">No warehouses found.</td>
              </tr>
            } @else {
              @for (warehouse of filteredWarehouses(); track warehouse.id) {
                <tr class="animate-fade-in">
                  <td><strong>{{ warehouse.code }}</strong></td>
                  <td>{{ warehouse.name }}</td>
                  <td>{{ warehouse.address || '-' }}</td>
                  <td>
                    <span class="badge" [class.badge-status-todo]="!warehouse.isActive" [class.badge-status-progress]="warehouse.isActive" style="background-color: warehouse.isActive ? 'var(--success-bg)' : 'var(--border)'; color: warehouse.isActive ? 'var(--success)' : 'var(--text-muted)'">
                      {{ warehouse.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text" (click)="openEditModal(warehouse)" title="Edit">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text" (click)="onDelete(warehouse.id)" title="Delete">
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
              <h3>{{ isEditMode() ? 'Edit Warehouse' : 'Create New Warehouse' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #warehouseForm="ngForm" (ngSubmit)="onSubmit(warehouseForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="wh-code">Warehouse Code</label>
                  <input 
                    type="text" 
                    id="wh-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentWarehouse.code" 
                    #whCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. WH-MAIN"
                    [disabled]="isEditMode()"
                  />
                  @if (whCode.invalid && (whCode.dirty || whCode.touched)) {
                    <span class="error-text">Warehouse code is required.</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="wh-name">Warehouse Name</label>
                  <input 
                    type="text" 
                    id="wh-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentWarehouse.name" 
                    #whName="ngModel" 
                    required 
                    maxlength="200"
                    placeholder="e.g. Main Warehouse"
                  />
                  @if (whName.invalid && (whName.dirty || whName.touched)) {
                    <span class="error-text">Warehouse name is required.</span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="wh-address">Address</label>
                  <input 
                    type="text" 
                    id="wh-address" 
                    name="address" 
                    class="form-input" 
                    [(ngModel)]="currentWarehouse.address" 
                    maxlength="500"
                    placeholder="e.g. Warehouse Block C, Industrial Zone"
                  />
                </div>

                <div class="form-group flex align-center gap-8 mt-16">
                  <input 
                    type="checkbox" 
                    id="wh-active" 
                    name="isActive" 
                    [(ngModel)]="currentWarehouse.isActive"
                  />
                  <label for="wh-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="warehouseForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Warehouse' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .warehouses-container {
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
export class Warehouses implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly toastService = inject(ToastService);

  protected warehouses = signal<any[]>([]);
  protected filteredWarehouses = signal<any[]>([]);
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentWarehouse = {
    id: '',
    code: '',
    name: '',
    address: '',
    isActive: true
  };

  ngOnInit() {
    this.loadWarehouses();
  }

  loadWarehouses() {
    this.loading.set(true);
    this.warehouseService.getAll().subscribe({
      next: (data) => {
        this.warehouses.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load warehouses.');
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredWarehouses.set(this.warehouses());
    } else {
      this.filteredWarehouses.set(
        this.warehouses().filter(w => 
          w.code.toLowerCase().includes(query) || 
          w.name.toLowerCase().includes(query) ||
          (w.address && w.address.toLowerCase().includes(query))
        )
      );
    }
  }

  onSearchChange() {
    this.applyFilter();
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentWarehouse = {
      id: '',
      code: '',
      name: '',
      address: '',
      isActive: true
    };
    this.showModal.set(true);
  }

  openEditModal(warehouse: any) {
    this.isEditMode.set(true);
    this.currentWarehouse = { ...warehouse };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.warehouseService.update(this.currentWarehouse.id, this.currentWarehouse).subscribe({
        next: () => {
          this.toastService.success('Warehouse updated successfully.');
          this.closeModal();
          this.loadWarehouses();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update warehouse.');
          this.submitting.set(false);
        }
      });
    } else {
      this.warehouseService.create(this.currentWarehouse).subscribe({
        next: () => {
          this.toastService.success('Warehouse created successfully.');
          this.closeModal();
          this.loadWarehouses();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create warehouse.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this warehouse?')) {
      this.loading.set(true);
      this.warehouseService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Warehouse deleted successfully.');
          this.loadWarehouses();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete warehouse.');
          this.loading.set(false);
        }
      });
    }
  }
}

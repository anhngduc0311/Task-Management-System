import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SupplierService } from '../../core/services/supplier.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="suppliers-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search suppliers..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Supplier
        </button>
      </div>

      <!-- Suppliers List/Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Contact Person</th>
              <th>Phone / Email</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (filteredSuppliers().length === 0) {
              <tr>
                <td colspan="6" class="text-center text-muted py-24">No suppliers found.</td>
              </tr>
            } @else {
              @for (supplier of filteredSuppliers(); track supplier.id) {
                <tr class="animate-fade-in">
                  <td><strong>{{ supplier.code }}</strong></td>
                  <td>
                    <div>{{ supplier.name }}</div>
                    @if (supplier.taxCode) {
                      <div class="sub-text">Tax: {{ supplier.taxCode }}</div>
                    }
                  </td>
                  <td>{{ supplier.contactPerson || '-' }}</td>
                  <td>
                    <div>{{ supplier.phone || '-' }}</div>
                    @if (supplier.email) {
                      <div class="sub-text">{{ supplier.email }}</div>
                    }
                  </td>
                  <td>
                    <span class="badge" [class.badge-status-todo]="!supplier.isActive" [class.badge-status-progress]="supplier.isActive" style="background-color: supplier.isActive ? 'var(--success-bg)' : 'var(--border)'; color: supplier.isActive ? 'var(--success)' : 'var(--text-muted)'">
                      {{ supplier.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text" (click)="openEditModal(supplier)" title="Edit">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text" (click)="onDelete(supplier.id)" title="Delete">
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
          <div class="modal-container modal-lg animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ isEditMode() ? 'Edit Supplier' : 'Create New Supplier' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #supplierForm="ngForm" (ngSubmit)="onSubmit(supplierForm)">
              <div class="modal-body grid grid-cols-2 gap-16">
                <div class="form-group">
                  <label class="form-label" for="sup-code">Supplier Code</label>
                  <input 
                    type="text" 
                    id="sup-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.code" 
                    #supCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. SUP-001"
                    [disabled]="isEditMode()"
                  />
                  @if (supCode.invalid && (supCode.dirty || supCode.touched)) {
                    <span class="error-text">Supplier code is required.</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="sup-name">Supplier Name</label>
                  <input 
                    type="text" 
                    id="sup-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.name" 
                    #supName="ngModel" 
                    required 
                    maxlength="200"
                    placeholder="e.g. ACME Corp"
                  />
                  @if (supName.invalid && (supName.dirty || supName.touched)) {
                    <span class="error-text">Supplier name is required.</span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="sup-contact">Contact Person</label>
                  <input 
                    type="text" 
                    id="sup-contact" 
                    name="contactPerson" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.contactPerson" 
                    maxlength="100"
                    placeholder="e.g. John Doe"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="sup-tax">Tax Code</label>
                  <input 
                    type="text" 
                    id="sup-tax" 
                    name="taxCode" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.taxCode" 
                    maxlength="50"
                    placeholder="e.g. 0102030405"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="sup-phone">Phone Number</label>
                  <input 
                    type="text" 
                    id="sup-phone" 
                    name="phone" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.phone" 
                    maxlength="20"
                    placeholder="e.g. +84 901234567"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="sup-email">Email Address</label>
                  <input 
                    type="email" 
                    id="sup-email" 
                    name="email" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.email" 
                    maxlength="100"
                    placeholder="e.g. contact@acme.com"
                  />
                </div>

                <div class="form-group col-span-2">
                  <label class="form-label" for="sup-address">Address</label>
                  <input 
                    type="text" 
                    id="sup-address" 
                    name="address" 
                    class="form-input" 
                    [(ngModel)]="currentSupplier.address" 
                    maxlength="500"
                    placeholder="e.g. 123 Business Rd, Ward 4, District 1, HCMC"
                  />
                </div>

                <div class="form-group col-span-2 flex align-center gap-8 mt-8">
                  <input 
                    type="checkbox" 
                    id="sup-active" 
                    name="isActive" 
                    [(ngModel)]="currentSupplier.isActive"
                  />
                  <label for="sup-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="supplierForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Supplier' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .suppliers-container {
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
    .sub-text {
      font-size: 0.75rem;
      color: var(--text-muted);
      margin-top: 2px;
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
    
    .grid { display: grid; }
    .grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .gap-16 { gap: 16px; }
    .col-span-2 { grid-column: span 2 / span 2; }
    .modal-lg {
      max-width: 680px;
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
    .gap-8 { gap: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    .text-center { text-align: center; }
    .mb-0 { margin-bottom: 0; }
  `]
})
export class Suppliers implements OnInit {
  private readonly supplierService = inject(SupplierService);
  private readonly toastService = inject(ToastService);

  protected suppliers = signal<any[]>([]);
  protected filteredSuppliers = signal<any[]>([]);
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentSupplier = {
    id: '',
    code: '',
    name: '',
    phone: '',
    email: '',
    address: '',
    taxCode: '',
    contactPerson: '',
    isActive: true
  };

  ngOnInit() {
    this.loadSuppliers();
  }

  loadSuppliers() {
    this.loading.set(true);
    this.supplierService.getAll().subscribe({
      next: (data) => {
        this.suppliers.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load suppliers.');
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredSuppliers.set(this.suppliers());
    } else {
      this.filteredSuppliers.set(
        this.suppliers().filter(s => 
          s.code.toLowerCase().includes(query) || 
          s.name.toLowerCase().includes(query) ||
          (s.contactPerson && s.contactPerson.toLowerCase().includes(query))
        )
      );
    }
  }

  onSearchChange() {
    this.applyFilter();
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentSupplier = {
      id: '',
      code: '',
      name: '',
      phone: '',
      email: '',
      address: '',
      taxCode: '',
      contactPerson: '',
      isActive: true
    };
    this.showModal.set(true);
  }

  openEditModal(supplier: any) {
    this.isEditMode.set(true);
    this.currentSupplier = { ...supplier };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.supplierService.update(this.currentSupplier.id, this.currentSupplier).subscribe({
        next: () => {
          this.toastService.success('Supplier updated successfully.');
          this.closeModal();
          this.loadSuppliers();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update supplier.');
          this.submitting.set(false);
        }
      });
    } else {
      this.supplierService.create(this.currentSupplier).subscribe({
        next: () => {
          this.toastService.success('Supplier created successfully.');
          this.closeModal();
          this.loadSuppliers();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create supplier.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this supplier?')) {
      this.loading.set(true);
      this.supplierService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Supplier deleted successfully.');
          this.loadSuppliers();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete supplier.');
          this.loading.set(false);
        }
      });
    }
  }
}

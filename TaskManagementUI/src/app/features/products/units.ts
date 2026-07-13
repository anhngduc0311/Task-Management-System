import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UnitService } from '../../core/services/unit.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-units',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="units-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search units..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Unit
        </button>
      </div>

      <!-- Units List/Table -->
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
            @if (filteredUnits().length === 0) {
              <tr>
                <td colspan="4" class="text-center text-muted py-24">No units found.</td>
              </tr>
            } @else {
              @for (unit of filteredUnits(); track unit.id) {
                <tr class="animate-fade-in">
                  <td><strong>{{ unit.code }}</strong></td>
                  <td>{{ unit.name }}</td>
                  <td>
                    <span class="badge" [class.badge-status-todo]="!unit.isActive" [class.badge-status-progress]="unit.isActive" style="background-color: unit.isActive ? 'var(--success-bg)' : 'var(--border)'; color: unit.isActive ? 'var(--success)' : 'var(--text-muted)'">
                      {{ unit.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text" (click)="openEditModal(unit)" title="Edit">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text" (click)="onDelete(unit.id)" title="Delete">
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
              <h3>{{ isEditMode() ? 'Edit Unit' : 'Create New Unit' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #unitForm="ngForm" (ngSubmit)="onSubmit(unitForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="unit-code">Unit Code</label>
                  <input 
                    type="text" 
                    id="unit-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentUnit.code" 
                    #unitCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. PCS, BOX, KG"
                    [disabled]="isEditMode()"
                  />
                  @if (unitCode.invalid && (unitCode.dirty || unitCode.touched)) {
                    <span class="error-text">Unit code is required (max 50 chars).</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="unit-name">Unit Name</label>
                  <input 
                    type="text" 
                    id="unit-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentUnit.name" 
                    #unitName="ngModel" 
                    required 
                    maxlength="100"
                    placeholder="e.g. Piece, Box, Kilogram"
                  />
                  @if (unitName.invalid && (unitName.dirty || unitName.touched)) {
                    <span class="error-text">Unit name is required (max 100 chars).</span>
                  }
                </div>

                <div class="form-group flex align-center gap-8 mt-16">
                  <input 
                    type="checkbox" 
                    id="unit-active" 
                    name="isActive" 
                    [(ngModel)]="currentUnit.isActive"
                  />
                  <label for="unit-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="unitForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Unit' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .units-container {
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
export class Units implements OnInit {
  private readonly unitService = inject(UnitService);
  private readonly toastService = inject(ToastService);

  protected units = signal<any[]>([]);
  protected filteredUnits = signal<any[]>([]);
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentUnit = {
    id: '',
    code: '',
    name: '',
    isActive: true
  };

  ngOnInit() {
    this.loadUnits();
  }

  loadUnits() {
    this.loading.set(true);
    this.unitService.getAll().subscribe({
      next: (data) => {
        this.units.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load units.');
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) {
      this.filteredUnits.set(this.units());
    } else {
      this.filteredUnits.set(
        this.units().filter(u => 
          u.code.toLowerCase().includes(query) || 
          u.name.toLowerCase().includes(query)
        )
      );
    }
  }

  onSearchChange() {
    this.applyFilter();
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentUnit = {
      id: '',
      code: '',
      name: '',
      isActive: true
    };
    this.showModal.set(true);
  }

  openEditModal(unit: any) {
    this.isEditMode.set(true);
    this.currentUnit = { ...unit };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.unitService.update(this.currentUnit.id, this.currentUnit).subscribe({
        next: () => {
          this.toastService.success('Unit updated successfully.');
          this.closeModal();
          this.loadUnits();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update unit.');
          this.submitting.set(false);
        }
      });
    } else {
      this.unitService.create(this.currentUnit).subscribe({
        next: () => {
          this.toastService.success('Unit created successfully.');
          this.closeModal();
          this.loadUnits();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create unit.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this unit?')) {
      this.loading.set(true);
      this.unitService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Unit deleted successfully.');
          this.loadUnits();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete unit.');
          this.loading.set(false);
        }
      });
    }
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReceiptService } from '../../core/services/receipt.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { SupplierService } from '../../core/services/supplier.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LoadingSpinner],
  template: `
    <div class="receipts-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Tabs header -->
      <div class="tabs-header mb-24">
        <button class="tab-btn" [class.active]="activeTab() === 'import'" (click)="setTab('import')">Import Receipts</button>
        <button class="tab-btn" [class.active]="activeTab() === 'export'" (click)="setTab('export')">Export Receipts</button>
        <button class="tab-btn" [class.active]="activeTab() === 'transfer'" (click)="setTab('transfer')">Transfer Receipts</button>
      </div>

      <!-- Filters & Actions -->
      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="flex gap-16 flex-wrap align-center">
          <!-- Warehouse Filter -->
          <div class="filter-group">
            <select class="form-select" [(ngModel)]="filters.warehouseId" (change)="onFilterChange()" style="min-width: 180px;">
              <option value="">All Warehouses</option>
              @for (wh of warehouses(); track wh.id) {
                <option [value]="wh.id">{{ wh.name }}</option>
              }
            </select>
          </div>

          <!-- Supplier Filter (only for Import) -->
          <div class="filter-group" *ngIf="activeTab() === 'import'">
            <select class="form-select" [(ngModel)]="filters.supplierId" (change)="onFilterChange()" style="min-width: 180px;">
              <option value="">All Suppliers</option>
              @for (sup of suppliers(); track sup.id) {
                <option [value]="sup.id">{{ sup.name }}</option>
              }
            </select>
          </div>

          <!-- Status Filter -->
          <div class="filter-group">
            <select class="form-select" [(ngModel)]="filters.status" (change)="onFilterChange()" style="min-width: 140px;">
              <option value="">All Statuses</option>
              <option value="Draft">Draft</option>
              <option value="Confirmed">Confirmed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>
        </div>

        <button class="btn btn-primary" (click)="goToCreate()">
          <span class="material-symbols-rounded">add</span>
          New {{ getActiveTypeName() }}
        </button>
      </div>

      <!-- Receipts Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Receipt No</th>
              <th>Warehouse</th>
              <th *ngIf="activeTab() === 'import'">Supplier</th>
              <th>Total Amount</th>
              <th>Status</th>
              <th>Created At</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (receipts().length === 0) {
              <tr>
                <td [attr.colspan]="activeTab() === 'import' ? 7 : 6" class="text-center text-muted py-24">No receipts found.</td>
              </tr>
            } @else {
              @for (r of receipts(); track r.id) {
                <tr class="animate-fade-in">
                  <td><span class="code-badge">{{ r.receiptNo }}</span></td>
                  <td>
                    @if (activeTab() === 'transfer') {
                      <span>{{ r.fromWarehouseName }} &rarr; {{ r.toWarehouseName }}</span>
                    } @else {
                      <span>{{ r.warehouseName }}</span>
                    }
                  </td>
                  <td *ngIf="activeTab() === 'import'">{{ r.supplierName || '-' }}</td>
                  <td>{{ r.totalAmount | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td>
                    <span class="badge" [ngClass]="'badge-status-' + getStatusClass(r.status)">
                      {{ getStatusText(r.status) }}
                    </span>
                  </td>
                  <td>{{ r.createdAt | date:'short' }}</td>
                  <td class="text-right actions-cell">
                    <a class="btn btn-text btn-sm" [routerLink]="['/inventory-receipts/edit', r.id]" [queryParams]="{ type: getActiveTypeName() }" title="Edit / View Details">
                      <span class="material-symbols-rounded text-primary">{{ r.status === 0 ? 'edit' : 'visibility' }}</span>
                    </a>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      @if (totalCount() > filters.pageSize) {
        <div class="flex justify-center align-center gap-16 mt-24">
          <button class="btn btn-outline" [disabled]="filters.page === 1" (click)="goToPage(filters.page - 1)">
            Previous
          </button>
          <span class="page-indicator">Page {{ filters.page }} of {{ totalPages() }}</span>
          <button class="btn btn-outline" [disabled]="filters.page === totalPages()" (click)="goToPage(filters.page + 1)">
            Next
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .receipts-container {
      width: 100%;
    }
    .tabs-header {
      display: flex;
      border-bottom: 1px solid var(--border);
      gap: 16px;
    }
    .tab-btn {
      background: none;
      border: none;
      padding: 10px 16px;
      font-size: 0.95rem;
      font-weight: 600;
      color: var(--text-muted);
      cursor: pointer;
      position: relative;
      transition: color var(--transition-fast);
    }
    .tab-btn:hover {
      color: var(--text-main);
    }
    .tab-btn.active {
      color: var(--primary);
    }
    .tab-btn.active::after {
      content: '';
      position: absolute;
      bottom: -1px;
      left: 0;
      right: 0;
      height: 2px;
      background-color: var(--primary);
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
    .code-badge {
      font-family: monospace;
      font-weight: 700;
      background-color: #f1f5f9;
      color: #334155;
      padding: 4px 8px;
      border-radius: 6px;
      font-size: 0.8rem;
    }
    .text-right {
      text-align: right;
    }
    .text-center {
      text-align: center;
    }
    .actions-cell {
      display: flex;
      justify-content: flex-end;
      gap: 4px;
    }
    .text-primary {
      color: var(--primary);
    }
    .mb-24 { margin-bottom: 24px; }
    .flex { display: flex; }
    .justify-between { justify-content: space-between; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .flex-wrap { flex-wrap: wrap; }
    .gap-16 { gap: 16px; }
    .mt-24 { margin-top: 24px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    .page-indicator {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-muted);
    }
  `]
})
export class Receipts implements OnInit {
  private readonly receiptService = inject(ReceiptService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly supplierService = inject(SupplierService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected activeTab = signal<'import' | 'export' | 'transfer'>('import');
  protected loading = signal(false);

  protected receipts = signal<any[]>([]);
  protected totalCount = signal(0);

  // Filter Lookups
  protected warehouses = signal<any[]>([]);
  protected suppliers = signal<any[]>([]);

  protected filters = {
    warehouseId: '',
    supplierId: '',
    status: '', // Draft, Confirmed, Cancelled
    page: 1,
    pageSize: 10
  };

  ngOnInit() {
    this.warehouseService.getAll().subscribe(data => this.warehouses.set(data.filter(x => x.isActive)));
    this.supplierService.getAll().subscribe(data => this.suppliers.set(data.filter(x => x.isActive)));
    this.loadReceipts();
  }

  setTab(tab: 'import' | 'export' | 'transfer') {
    this.activeTab.set(tab);
    this.filters.page = 1;
    this.filters.warehouseId = '';
    this.filters.supplierId = '';
    this.filters.status = '';
    this.loadReceipts();
  }

  getActiveTypeName(): string {
    const t = this.activeTab();
    return t.charAt(0).toUpperCase() + t.slice(1);
  }

  goToCreate() {
    this.router.navigate(['/inventory-receipts/new'], { queryParams: { type: this.getActiveTypeName() } });
  }

  loadReceipts() {
    this.loading.set(true);
    
    // Status DTO maps: 0 = Draft, 1 = Confirmed, 2 = Cancelled
    let statusNum: number | undefined;
    if (this.filters.status === 'Draft') statusNum = 0;
    else if (this.filters.status === 'Confirmed') statusNum = 1;
    else if (this.filters.status === 'Cancelled') statusNum = 2;

    const page = this.filters.page;
    const pageSize = this.filters.pageSize;

    if (this.activeTab() === 'import') {
      this.receiptService.getImportReceipts(
        page, 
        pageSize, 
        this.filters.warehouseId || undefined, 
        this.filters.supplierId || undefined, 
        statusNum
      ).subscribe({
        next: (res) => {
          this.receipts.set(res.items);
          this.totalCount.set(res.total);
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load import receipts.');
          this.loading.set(false);
        }
      });
    } else if (this.activeTab() === 'export') {
      this.receiptService.getExportReceipts(
        page, 
        pageSize, 
        this.filters.warehouseId || undefined, 
        statusNum
      ).subscribe({
        next: (res) => {
          this.receipts.set(res.items);
          this.totalCount.set(res.total);
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load export receipts.');
          this.loading.set(false);
        }
      });
    } else {
      // Transfer
      this.receiptService.getTransferReceipts(
        page, 
        pageSize, 
        this.filters.warehouseId || undefined, // fromWarehouse
        undefined, // toWarehouse
        statusNum
      ).subscribe({
        next: (res) => {
          this.receipts.set(res.items);
          this.totalCount.set(res.total);
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load transfer receipts.');
          this.loading.set(false);
        }
      });
    }
  }

  getStatusClass(status: number): string {
    if (status === 0) return 'todo';
    if (status === 1) return 'progress';
    return 'danger';
  }

  getStatusText(status: number): string {
    if (status === 0) return 'Draft';
    if (status === 1) return 'Confirmed';
    return 'Cancelled';
  }

  totalPages(): number {
    return Math.ceil(this.totalCount() / this.filters.pageSize);
  }

  onFilterChange() {
    this.filters.page = 1;
    this.loadReceipts();
  }

  goToPage(page: number) {
    this.filters.page = page;
    this.loadReceipts();
  }
}

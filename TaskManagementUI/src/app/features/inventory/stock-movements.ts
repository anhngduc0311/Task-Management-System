import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../core/services/stock.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-stock-movements',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="stock-movements-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Advanced Filter Panel -->
      <div class="glass-card mb-24 grid grid-cols-4 gap-16 align-end">
        <div class="form-group mb-0">
          <label class="form-label">Warehouse</label>
          <select class="form-select" [(ngModel)]="filters.warehouseId" (change)="onFilterChange()">
            <option value="">All Warehouses</option>
            @for (wh of warehouses(); track wh.id) {
              <option [value]="wh.id">{{ wh.code }} - {{ wh.name }}</option>
            }
          </select>
        </div>

        <div class="form-group mb-0">
          <label class="form-label">Product</label>
          <select class="form-select" [(ngModel)]="filters.productId" (change)="onFilterChange()">
            <option value="">All Products</option>
            @for (prod of products(); track prod.id) {
              <option [value]="prod.id">{{ prod.productCode }} - {{ prod.name }}</option>
            }
          </select>
        </div>

        <div class="form-group mb-0">
          <label class="form-label">Movement Type</label>
          <select class="form-select" [(ngModel)]="filters.movementType" (change)="onFilterChange()">
            <option value="">All Types</option>
            <option value="0">Import (+)</option>
            <option value="1">Export (-)</option>
            <option value="2">Transfer Out (-)</option>
            <option value="3">Transfer In (+)</option>
            <option value="4">Adjustment In (+)</option>
            <option value="5">Adjustment Out (-)</option>
          </select>
        </div>

        <div class="flex justify-end mb-0">
          <button class="btn btn-outline" (click)="resetFilters()">
            <span class="material-symbols-rounded">restart_alt</span>
            Reset Filters
          </button>
        </div>
      </div>

      <!-- Stock Movements Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Warehouse</th>
              <th>Product Code</th>
              <th>Product Name / SKU</th>
              <th>Type</th>
              <th>Quantity</th>
              <th>Ref No</th>
            </tr>
          </thead>
          <tbody>
            @if (movements().length === 0) {
              <tr>
                <td colspan="7" class="text-center text-muted py-24">No stock movements found.</td>
              </tr>
            } @else {
              @for (m of movements(); track m.id) {
                <tr class="animate-fade-in">
                  <td>{{ m.createdAt | date:'short' }}</td>
                  <td><strong>{{ m.warehouseCode }}</strong></td>
                  <td><span class="code-badge">{{ m.productCode }}</span></td>
                  <td>
                    <div>{{ m.productName }}</div>
                    @if (m.variantSKU) {
                      <div class="sub-text">SKU: {{ m.variantSKU }}</div>
                    }
                  </td>
                  <td>
                    <span class="badge" [ngClass]="'badge-status-' + getMovementClass(m.movementType)">
                      {{ getMovementText(m.movementType) }}
                    </span>
                  </td>
                  <td>
                    <span class="qty-change" [class.negative]="isNegativeMovement(m.movementType)">
                      {{ isNegativeMovement(m.movementType) ? '-' : '+' }}{{ m.quantity }}
                    </span>
                  </td>
                  <td>
                    <span class="ref-badge">{{ m.referenceNo || '-' }}</span>
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
    .stock-movements-container {
      width: 100%;
    }
    .grid { display: grid; }
    .grid-cols-4 { grid-template-columns: repeat(4, minmax(0, 1fr)); }
    .gap-16 { gap: 16px; }
    .mb-24 { margin-bottom: 24px; }
    .align-end { align-items: flex-end; }
    
    .flex { display: flex; }
    .justify-end { justify-content: flex-end; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .mt-24 { margin-top: 24px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    
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
      padding: 14px 20px;
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
    
    .code-badge, .ref-badge {
      font-family: monospace;
      font-weight: 700;
      background-color: #f1f5f9;
      color: #334155;
      padding: 4px 8px;
      border-radius: 6px;
      font-size: 0.8rem;
    }
    .ref-badge {
      background-color: #e0e7ff;
      color: #4f46e5;
    }
    .sub-text {
      font-size: 0.75rem;
      color: var(--text-muted);
    }
    .qty-change {
      font-weight: 700;
      font-size: 0.95rem;
      color: var(--success);
    }
    .qty-change.negative {
      color: var(--danger);
    }
    .text-center { text-align: center; }
    .page-indicator {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-muted);
    }
    .mb-0 { margin-bottom: 0; }
  `]
})
export class StockMovements implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly toastService = inject(ToastService);

  protected loading = signal(false);
  protected movements = signal<any[]>([]);
  protected totalCount = signal(0);

  // Lookups
  protected warehouses = signal<any[]>([]);
  protected products = signal<any[]>([]);

  protected filters = {
    warehouseId: '',
    productId: '',
    movementType: '',
    page: 1,
    pageSize: 10
  };

  ngOnInit() {
    this.warehouseService.getAll().subscribe(data => this.warehouses.set(data.filter(x => x.isActive)));
    this.productService.searchProducts({ page: 1, pageSize: 1000, status: 'Active' }).subscribe(res => this.products.set(res.items));
    this.loadMovements();
  }

  loadMovements() {
    this.loading.set(true);
    
    let typeNum: number | undefined;
    if (this.filters.movementType !== '') {
      typeNum = parseInt(this.filters.movementType, 10);
    }

    this.stockService.getStockMovements(
      this.filters.page,
      this.filters.pageSize,
      this.filters.warehouseId || undefined,
      this.filters.productId || undefined,
      typeNum
    ).subscribe({
      next: (res) => {
        this.movements.set(res.items);
        this.totalCount.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load stock movements.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange() {
    this.filters.page = 1;
    this.loadMovements();
  }

  resetFilters() {
    this.filters.warehouseId = '';
    this.filters.productId = '';
    this.filters.movementType = '';
    this.filters.page = 1;
    this.loadMovements();
  }

  totalPages(): number {
    return Math.ceil(this.totalCount() / this.filters.pageSize);
  }

  goToPage(page: number) {
    this.filters.page = page;
    this.loadMovements();
  }

  getMovementText(type: number): string {
    switch (type) {
      case 0: return 'Import';
      case 1: return 'Export';
      case 2: return 'Transfer Out';
      case 3: return 'Transfer In';
      case 4: return 'Adjustment In';
      case 5: return 'Adjustment Out';
      default: return 'Movement';
    }
  }

  getMovementClass(type: number): string {
    if (type === 0 || type === 3 || type === 4) return 'progress'; // positive / active
    return 'todo'; // negative / neutral / warning
  }

  isNegativeMovement(type: number): boolean {
    return type === 1 || type === 2 || type === 5;
  }
}

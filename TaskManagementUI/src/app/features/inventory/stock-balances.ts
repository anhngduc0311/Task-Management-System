import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../core/services/stock.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-stock-balances',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="stock-balances-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Search & Filters -->
      <div class="glass-card mb-24 grid grid-cols-3 gap-16 align-end">
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

        <div class="flex justify-end mb-0">
          <button class="btn btn-outline" (click)="resetFilters()">
            <span class="material-symbols-rounded">restart_alt</span>
            Reset Filters
          </button>
        </div>
      </div>

      <!-- Stock Balances Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Warehouse</th>
              <th>Product Code</th>
              <th>Product Name</th>
              <th>SKU (Variant)</th>
              <th>In Stock Qty</th>
              <th>Last Updated</th>
            </tr>
          </thead>
          <tbody>
            @if (balances().length === 0) {
              <tr>
                <td colspan="6" class="text-center text-muted py-24">No stock balances found.</td>
              </tr>
            } @else {
              @for (b of balances(); track b.id) {
                <tr class="animate-fade-in">
                  <td><strong>{{ b.warehouseCode }}</strong> - {{ b.warehouseName }}</td>
                  <td><span class="code-badge">{{ b.productCode }}</span></td>
                  <td>{{ b.productName }}</td>
                  <td>
                    @if (b.variantSKU) {
                      <span class="code-badge secondary-badge">{{ b.variantSKU }}</span>
                    } @else {
                      <span class="text-muted" style="font-size:0.85rem;">Base Product</span>
                    }
                  </td>
                  <td>
                    <span class="qty-badge" [class.out-of-stock]="b.quantity <= 0">
                      {{ b.quantity }}
                    </span>
                  </td>
                  <td>{{ b.lastUpdatedAt | date:'short' }}</td>
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
    .stock-balances-container {
      width: 100%;
    }
    .grid { display: grid; }
    .grid-cols-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .gap-16 { gap: 16px; }
    .mb-24 { margin-bottom: 24px; }
    .align-end { align-items: flex-end; }
    
    .flex { display: flex; }
    .justify-end { justify-content: flex-end; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .gap-16 { gap: 16px; }
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
    .secondary-badge {
      background-color: var(--primary-light);
      color: var(--primary);
    }
    .qty-badge {
      font-weight: 700;
      font-size: 0.9rem;
      color: var(--success);
    }
    .qty-badge.out-of-stock {
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
export class StockBalances implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly toastService = inject(ToastService);

  protected loading = signal(false);
  protected balances = signal<any[]>([]);
  protected totalCount = signal(0);

  // Lookups
  protected warehouses = signal<any[]>([]);
  protected products = signal<any[]>([]);

  protected filters = {
    warehouseId: '',
    productId: '',
    page: 1,
    pageSize: 10
  };

  ngOnInit() {
    this.warehouseService.getAll().subscribe(data => this.warehouses.set(data.filter(x => x.isActive)));
    this.productService.searchProducts({ page: 1, pageSize: 1000, status: 'Active' }).subscribe(res => this.products.set(res.items));
    this.loadBalances();
  }

  loadBalances() {
    this.loading.set(true);
    this.stockService.getStockBalances(
      this.filters.page,
      this.filters.pageSize,
      this.filters.warehouseId || undefined,
      this.filters.productId || undefined
    ).subscribe({
      next: (res) => {
        this.balances.set(res.items);
        this.totalCount.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load stock balances.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange() {
    this.filters.page = 1;
    this.loadBalances();
  }

  resetFilters() {
    this.filters.warehouseId = '';
    this.filters.productId = '';
    this.filters.page = 1;
    this.loadBalances();
  }

  totalPages(): number {
    return Math.ceil(this.totalCount() / this.filters.pageSize);
  }

  goToPage(page: number) {
    this.filters.page = page;
    this.loadBalances();
  }
}

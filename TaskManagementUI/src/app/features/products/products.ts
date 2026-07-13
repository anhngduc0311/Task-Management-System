import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { ProductCategoryService } from '../../core/services/product-category.service';
import { OriginService } from '../../core/services/origin.service';
import { SupplierService } from '../../core/services/supplier.service';
import { ProductLabelService } from '../../core/services/product-label.service';
import { StockService } from '../../core/services/stock.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LoadingSpinner],
  template: `
    <div class="products-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Advanced Filter Panel -->
      <div class="glass-card mb-24">
        <div class="flex justify-between align-center mb-16">
          <h3 style="font-size: 1.1rem; font-weight: 600;">Search & Filters</h3>
          <button class="btn btn-outline btn-sm" (click)="resetFilters()">
            <span class="material-symbols-rounded" style="font-size: 16px;">restart_alt</span>
            Reset
          </button>
        </div>

        <div class="grid grid-cols-4 gap-16">
          <!-- Text Search -->
          <div class="form-group mb-0">
            <label class="form-label">Search Text</label>
            <input 
              type="text" 
              class="form-input" 
              placeholder="Code, name..." 
              [(ngModel)]="filters.search"
              (input)="onFilterChange()"
            />
          </div>

          <!-- Category -->
          <div class="form-group mb-0">
            <label class="form-label">Category</label>
            <select class="form-select" [(ngModel)]="filters.categoryId" (change)="onFilterChange()">
              <option [value]="''">All Categories</option>
              @for (cat of categories(); track cat.id) {
                <option [value]="cat.id">{{ cat.code }} - {{ cat.name }}</option>
              }
            </select>
          </div>

          <!-- Status -->
          <div class="form-group mb-0">
            <label class="form-label">Status</label>
            <select class="form-select" [(ngModel)]="filters.status" (change)="onFilterChange()">
              <option [value]="''">All Statuses</option>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
              <option value="Draft">Draft</option>
            </select>
          </div>

          <!-- Origin -->
          <div class="form-group mb-0">
            <label class="form-label">Origin</label>
            <select class="form-select" [(ngModel)]="filters.originId" (change)="onFilterChange()">
              <option [value]="''">All Origins</option>
              @for (origin of origins(); track origin.id) {
                <option [value]="origin.id">{{ origin.code }} - {{ origin.name }}</option>
              }
            </select>
          </div>

          <!-- Supplier -->
          <div class="form-group mb-0">
            <label class="form-label">Supplier</label>
            <select class="form-select" [(ngModel)]="filters.supplierId" (change)="onFilterChange()">
              <option [value]="''">All Suppliers</option>
              @for (sup of suppliers(); track sup.id) {
                <option [value]="sup.id">{{ sup.code }} - {{ sup.name }}</option>
              }
            </select>
          </div>

          <!-- Label -->
          <div class="form-group mb-0">
            <label class="form-label">Label</label>
            <select class="form-select" [(ngModel)]="filters.labelId" (change)="onFilterChange()">
              <option [value]="''">All Labels</option>
              @for (lbl of labels(); track lbl.id) {
                <option [value]="lbl.id">{{ lbl.name }}</option>
              }
            </select>
          </div>

          <!-- Min Price -->
          <div class="form-group mb-0">
            <label class="form-label">Min Price</label>
            <input 
              type="number" 
              class="form-input" 
              placeholder="0.00" 
              [(ngModel)]="filters.minPrice"
              (change)="onFilterChange()"
            />
          </div>

          <!-- Max Price -->
          <div class="form-group mb-0">
            <label class="form-label">Max Price</label>
            <input 
              type="number" 
              class="form-input" 
              placeholder="Max" 
              [(ngModel)]="filters.maxPrice"
              (change)="onFilterChange()"
            />
          </div>
        </div>

        <div class="flex justify-between align-center mt-16 pt-16 border-top flex-wrap gap-12">
          <!-- Sort -->
          <div class="flex align-center gap-8">
            <label class="form-label mb-0" style="white-space: nowrap;">Sort By</label>
            <select class="form-select" style="width: 140px; padding: 6px 12px; font-size: 0.85rem;" [(ngModel)]="filters.sortBy" (change)="onFilterChange()">
              <option value="createdat">Created Date</option>
              <option value="name">Product Name</option>
              <option value="productcode">Product Code</option>
              <option value="defaultprice">Price</option>
            </select>
            <button class="btn btn-outline btn-sm" style="padding: 6px;" (click)="toggleSortDirection()" title="Toggle direction">
              <span class="material-symbols-rounded" style="font-size: 18px;">
                {{ filters.sortDescending ? 'arrow_downward' : 'arrow_upward' }}
              </span>
            </button>
          </div>

          <button class="btn btn-primary" routerLink="/products/new">
            <span class="material-symbols-rounded">add</span>
            New Product
          </button>
        </div>
      </div>

      <!-- Products Grid/Table -->
      <div class="glass-card table-card overflow-x">
        <table class="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Category</th>
              <th>Default Price</th>
              <th>Total Stock</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @if (products().length === 0) {
              <tr>
                <td colspan="7" class="text-center text-muted py-24">No products found.</td>
              </tr>
            } @else {
              @for (p of products(); track p.id) {
                <tr class="animate-fade-in">
                  <td><span class="code-badge">{{ p.productCode }}</span></td>
                  <td>
                    <div style="font-weight: 600;">{{ p.name }}</div>
                    @if (p.originName) {
                      <div class="sub-text">Origin: {{ p.originName }}</div>
                    }
                  </td>
                  <td>{{ p.categoryName || '-' }}</td>
                  <td>{{ p.defaultPrice | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td>
                    <span class="stock-badge" [class.low-stock]="getProductStock(p.id) === 0">
                      {{ getProductStock(p.id) }} {{ p.baseUnitName }}
                    </span>
                  </td>
                  <td>
                    <span class="badge" [ngClass]="'badge-status-' + p.status.toLowerCase()">
                      {{ p.status }}
                    </span>
                  </td>
                  <td class="text-right actions-cell">
                    <a class="btn btn-text" [routerLink]="['/products/edit', p.id]" title="Edit Product">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </a>
                    <button class="btn btn-text" (click)="onDelete(p.id)" title="Delete Product">
                      <span class="material-symbols-rounded text-danger">delete</span>
                    </button>
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
    .products-container {
      width: 100%;
    }
    .grid { display: grid; }
    .grid-cols-4 { grid-template-columns: repeat(4, minmax(0, 1fr)); }
    .gap-16 { gap: 16px; }
    .gap-8 { gap: 8px; }
    .mb-24 { margin-bottom: 24px; }
    .mb-16 { margin-bottom: 16px; }
    .mt-16 { margin-top: 16px; }
    .mt-24 { margin-top: 24px; }
    .pt-16 { padding-top: 16px; }
    .pb-8 { padding-bottom: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    
    .border-top {
      border-top: 1px solid var(--border);
    }
    
    .flex { display: flex; }
    .justify-between { justify-content: space-between; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .align-end { align-items: flex-end; }
    .flex-wrap { flex-wrap: wrap; }
    
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
    .stock-badge {
      display: inline-block;
      font-weight: 600;
      font-size: 0.85rem;
      background-color: var(--success-bg);
      color: var(--success);
      padding: 4px 8px;
      border-radius: 6px;
    }
    .stock-badge.low-stock {
      background-color: var(--danger-bg);
      color: var(--danger);
    }
    .sub-text {
      font-size: 0.75rem;
      color: var(--text-muted);
      margin-top: 2px;
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
    .text-danger {
      color: var(--danger);
    }
    .page-indicator {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-muted);
    }
    .mb-0 { margin-bottom: 0; }
  `]
})
export class Products implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(ProductCategoryService);
  private readonly originService = inject(OriginService);
  private readonly supplierService = inject(SupplierService);
  private readonly labelService = inject(ProductLabelService);
  private readonly stockService = inject(StockService);
  private readonly toastService = inject(ToastService);

  protected products = signal<any[]>([]);
  protected totalCount = signal(0);
  protected loading = signal(false);

  // Lookups
  protected categories = signal<any[]>([]);
  protected origins = signal<any[]>([]);
  protected suppliers = signal<any[]>([]);
  protected labels = signal<any[]>([]);
  
  // Stock Balances cache (mapped by productId)
  protected stockCache = signal<Map<string, number>>(new Map());

  protected filters = {
    search: '',
    categoryId: '',
    includeChildCategories: true,
    status: '',
    originId: '',
    supplierId: '',
    labelId: '',
    minPrice: null as number | null,
    maxPrice: null as number | null,
    sortBy: 'createdat',
    sortDescending: true,
    page: 1,
    pageSize: 10
  };

  ngOnInit() {
    this.loadLookups();
    this.loadProducts();
  }

  loadLookups() {
    this.categoryService.getTree().subscribe(data => {
      // Flatten for category selection
      const list: any[] = [];
      function recurse(nodes: any[]) {
        for (const node of nodes) {
          list.push(node);
          if (node.children) recurse(node.children);
        }
      }
      recurse(data);
      this.categories.set(list);
    });

    this.originService.getAll().subscribe(data => this.origins.set(data.filter(x => x.isActive)));
    this.supplierService.getAll().subscribe(data => this.suppliers.set(data.filter(x => x.isActive)));
    this.labelService.getAll().subscribe(data => this.labels.set(data.filter(x => x.isActive)));
  }

  loadProducts() {
    this.loading.set(true);

    const searchPayload = {
      search: this.filters.search || undefined,
      categoryId: this.filters.categoryId || undefined,
      includeChildCategories: this.filters.categoryId ? this.filters.includeChildCategories : undefined,
      status: this.filters.status || undefined,
      originId: this.filters.originId || undefined,
      supplierId: this.filters.supplierId || undefined,
      labelId: this.filters.labelId || undefined,
      minPrice: this.filters.minPrice !== null ? this.filters.minPrice : undefined,
      maxPrice: this.filters.maxPrice !== null ? this.filters.maxPrice : undefined,
      sortBy: this.filters.sortBy,
      sortDescending: this.filters.sortDescending,
      page: this.filters.page,
      pageSize: this.filters.pageSize
    };

    this.productService.searchProducts(searchPayload).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loadStocks(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load products.');
        this.loading.set(false);
      }
    });
  }

  loadStocks(productsList: any[]) {
    if (productsList.length === 0) return;
    
    // We can query stock-balances to update the stockCache
    this.stockService.getStockBalances(1, 100).subscribe({
      next: (res) => {
        const cache = new Map<string, number>();
        // Sum stock balance for each product across warehouses and variants
        res.items.forEach((item: any) => {
          const prev = cache.get(item.productId) || 0;
          cache.set(item.productId, prev + item.quantity);
        });
        this.stockCache.set(cache);
      }
    });
  }

  getProductStock(productId: string): number {
    return this.stockCache().get(productId) || 0;
  }

  totalPages(): number {
    return Math.ceil(this.totalCount() / this.filters.pageSize);
  }

  onFilterChange() {
    this.filters.page = 1;
    this.loadProducts();
  }

  resetFilters() {
    this.filters = {
      search: '',
      categoryId: '',
      includeChildCategories: true,
      status: '',
      originId: '',
      supplierId: '',
      labelId: '',
      minPrice: null,
      maxPrice: null,
      sortBy: 'createdat',
      sortDescending: true,
      page: 1,
      pageSize: 10
    };
    this.loadProducts();
  }

  toggleSortDirection() {
    this.filters.sortDescending = !this.filters.sortDescending;
    this.loadProducts();
  }

  goToPage(page: number) {
    this.filters.page = page;
    this.loadProducts();
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this product? This will perform a soft delete.')) {
      this.loading.set(true);
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          this.toastService.success('Product deleted successfully.');
          this.loadProducts();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete product.');
          this.loading.set(false);
        }
      });
    }
  }
}

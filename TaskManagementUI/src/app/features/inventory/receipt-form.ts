import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { ReceiptService } from '../../core/services/receipt.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { SupplierService } from '../../core/services/supplier.service';
import { ProductService } from '../../core/services/product.service';
import { UnitService } from '../../core/services/unit.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

interface ReceiptLineItem {
  productId: string;
  productCode: string;
  productName: string;
  productVariantId: string | null;
  quantity: number;
  unitId: string;
  unitCode: string;
  unitPrice: number;
  amount: number;
  
  // UI helpers
  productSearch: string;
  showSearchDropdown: boolean;
  searchResults: any[];
  variants: any[];
  conversions: any[];
  baseUnitId: string;
  baseUnitCode: string;
}

@Component({
  selector: 'app-receipt-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LoadingSpinner],
  template: `
    <div class="receipt-form-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div>
          <a routerLink="/inventory-receipts" class="back-link flex align-center gap-8 mb-4">
            <span class="material-symbols-rounded">arrow_back</span>
            Back to Inventory
          </a>
          <h2 style="font-size: 1.5rem; font-weight: 700;">
            {{ isEditMode() ? 'Receipt Details / Edit' : 'Create ' + type() + ' Receipt' }}
          </h2>
        </div>

        <div class="flex gap-8" *ngIf="isEditMode() && getStatusText() === 'Draft'">
          <button class="btn btn-secondary" (click)="onConfirm()">
            <span class="material-symbols-rounded">check_circle</span>
            Confirm Receipt
          </button>
          <button class="btn btn-danger" (click)="onCancel()">
            <span class="material-symbols-rounded">cancel</span>
            Cancel Receipt
          </button>
        </div>
        <div *ngIf="isEditMode() && getStatusText() !== 'Draft'">
          <span class="badge" [class.badge-status-progress]="getStatusText() === 'Confirmed'" [class.badge-status-critical]="getStatusText() === 'Cancelled'" style="font-size: 0.9rem; padding: 6px 12px;">
            Status: {{ getStatusText() }}
          </span>
        </div>
      </div>

      <form #receiptForm="ngForm" (ngSubmit)="onSubmit(receiptForm)">
        <div class="glass-card mb-24 grid grid-cols-3 gap-24">
          <div class="col-span-2 grid grid-cols-2 gap-16">
            <!-- Receipt Number (Read Only) -->
            <div class="form-group" *ngIf="isEditMode()">
              <label class="form-label">Receipt No</label>
              <input type="text" class="form-input" [value]="receiptNo()" disabled />
            </div>

            <!-- Warehouse selection -->
            <div class="form-group" *ngIf="type() !== 'Transfer'">
              <label class="form-label">Warehouse</label>
              <select class="form-select" name="warehouseId" [(ngModel)]="receiptData.warehouseId" required [disabled]="isLocked()">
                <option value="" disabled>-- Select Warehouse --</option>
                @for (wh of warehouses(); track wh.id) {
                  <option [value]="wh.id">{{ wh.code }} - {{ wh.name }}</option>
                }
              </select>
            </div>

            <!-- From Warehouse (Transfer) -->
            <div class="form-group" *ngIf="type() === 'Transfer'">
              <label class="form-label">Source Warehouse (From)</label>
              <select class="form-select" name="fromWarehouseId" [(ngModel)]="receiptData.fromWarehouseId" required [disabled]="isLocked()" (change)="validateWarehouses()">
                <option value="" disabled>-- Select From Warehouse --</option>
                @for (wh of warehouses(); track wh.id) {
                  <option [value]="wh.id">{{ wh.code }} - {{ wh.name }}</option>
                }
              </select>
            </div>

            <!-- To Warehouse (Transfer) -->
            <div class="form-group" *ngIf="type() === 'Transfer'">
              <label class="form-label">Destination Warehouse (To)</label>
              <select class="form-select" name="toWarehouseId" [(ngModel)]="receiptData.toWarehouseId" required [disabled]="isLocked()" (change)="validateWarehouses()">
                <option value="" disabled>-- Select To Warehouse --</option>
                @for (wh of warehouses(); track wh.id) {
                  <option [value]="wh.id">{{ wh.code }} - {{ wh.name }}</option>
                }
              </select>
            </div>

            <!-- Supplier (Only for Import) -->
            <div class="form-group" *ngIf="type() === 'Import'">
              <label class="form-label">Supplier</label>
              <select class="form-select" name="supplierId" [(ngModel)]="receiptData.supplierId" [disabled]="isLocked()">
                <option [value]="''">-- None --</option>
                @for (sup of suppliers(); track sup.id) {
                  <option [value]="sup.id">{{ sup.code }} - {{ sup.name }}</option>
                }
              </select>
            </div>
          </div>

          <!-- Description and Total -->
          <div class="col-span-1 border-left pl-24 flex flex-col justify-between">
            <div class="form-group mb-0">
              <label class="form-label">Description / Remarks</label>
              <textarea class="form-input form-textarea" name="description" [(ngModel)]="receiptData.description" style="min-height: 80px;" placeholder="Add remarks..." [disabled]="isLocked()"></textarea>
            </div>

            <div class="total-summary mt-16" *ngIf="type() !== 'Transfer'">
              <span class="text-muted" style="font-size: 0.85rem; font-weight: 500;">Total Amount</span>
              <div class="price-val" style="font-size: 1.5rem; font-weight: 700; color: var(--primary);">
                {{ grandTotal() | currency:'USD':'symbol':'1.2-2' }}
              </div>
            </div>
          </div>
        </div>

        <!-- Lines Items Table -->
        <div class="glass-card mb-24 table-card overflow-x">
          <div class="flex justify-between align-center p-20 border-bottom">
            <h3 style="font-size: 1.1rem; font-weight: 600;">Line Items</h3>
            <button type="button" class="btn btn-outline btn-sm" (click)="addLineItem()" [disabled]="isLocked()">
              <span class="material-symbols-rounded">add</span>
              Add Line
            </button>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Product Selection</th>
                <th>Variant</th>
                <th>Unit</th>
                <th>Quantity</th>
                <th *ngIf="type() !== 'Transfer'">Unit Price</th>
                <th *ngIf="type() !== 'Transfer'">Total Amount</th>
                <th class="text-center" *ngIf="!isLocked()">Delete</th>
              </tr>
            </thead>
            <tbody>
              @if (lines().length === 0) {
                <tr>
                  <td [attr.colspan]="type() === 'Transfer' ? 5 : 7" class="text-center text-muted py-24">No items added. Click "Add Line" to add products.</td>
                </tr>
              } @else {
                @for (item of lines(); track i; let i = $index) {
                  <tr>
                    <!-- Product autocomplete search -->
                    <td style="position: relative; width: 280px;">
                      <div class="search-wrapper" *ngIf="!isLocked(); else lockedProduct">
                        <input 
                          type="text" 
                          class="form-input" 
                          placeholder="Search product..." 
                          [(ngModel)]="item.productSearch"
                          [name]="'search_' + i"
                          (input)="searchProduct(item)"
                          (focus)="searchProduct(item)"
                        />
                        @if (item.showSearchDropdown && item.searchResults.length > 0) {
                          <div class="autocomplete-dropdown">
                            @for (p of item.searchResults; track p.id) {
                              <div class="dropdown-item" (click)="selectProduct(item, p)">
                                <strong>{{ p.productCode }}</strong> - {{ p.name }}
                              </div>
                            }
                          </div>
                        }
                      </div>
                      <ng-template #lockedProduct>
                        <strong>{{ item.productCode }}</strong> - {{ item.productName }}
                      </ng-template>
                    </td>

                    <!-- Variant selector -->
                    <td>
                      @if (item.variants && item.variants.length > 0) {
                        <select class="form-select" [name]="'variant_' + i" [(ngModel)]="item.productVariantId" (change)="onVariantChange(item)" [disabled]="isLocked()">
                          <option [value]="null">Default</option>
                          @for (v of item.variants; track v.id) {
                            <option [value]="v.id">{{ v.attributeValueCombinations }} ({{ v.sku }})</option>
                          }
                        </select>
                      } @else {
                        <span class="text-muted" style="font-size: 0.85rem;">No variants</span>
                      }
                    </td>

                    <!-- Unit selection -->
                    <td>
                      <select class="form-select" [name]="'unit_' + i" [(ngModel)]="item.unitId" (change)="onUnitChange(item)" [disabled]="isLocked()">
                        <option [value]="item.baseUnitId">{{ item.baseUnitCode }}</option>
                        @for (c of item.conversions; track c.id) {
                          <option [value]="c.fromUnitId">{{ c.fromUnitName }}</option>
                        }
                      </select>
                      <!-- Conversion helper text -->
                      @if (item.unitId !== item.baseUnitId && getConversionRate(item) > 0) {
                        <div class="conversion-helper-text">
                          Rate: 1 = {{ getConversionRate(item) }} {{ item.baseUnitCode }}
                        </div>
                      }
                    </td>

                    <!-- Quantity -->
                    <td>
                      <input 
                        type="number" 
                        class="form-input text-right" 
                        style="width: 100px;" 
                        [name]="'qty_' + i" 
                        [(ngModel)]="item.quantity" 
                        (input)="recalculateLine(item)"
                        required
                        min="1"
                        [disabled]="isLocked()"
                      />
                    </td>

                    <!-- Unit Price (Hide on transfer) -->
                    <td *ngIf="type() !== 'Transfer'">
                      <input 
                        type="number" 
                        class="form-input text-right" 
                        style="width: 110px;" 
                        [name]="'price_' + i" 
                        [(ngModel)]="item.unitPrice" 
                        (input)="recalculateLine(item)"
                        required
                        min="0"
                        [disabled]="isLocked()"
                      />
                    </td>

                    <!-- Line Amount (Hide on transfer) -->
                    <td *ngIf="type() !== 'Transfer'">
                      <strong>{{ item.amount | currency:'USD':'symbol':'1.2-2' }}</strong>
                    </td>

                    <!-- Delete button -->
                    <td class="text-center" *ngIf="!isLocked()">
                      <button type="button" class="btn btn-text text-danger btn-sm" (click)="removeLineItem(i)" title="Remove Item">
                        <span class="material-symbols-rounded">delete</span>
                      </button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        <!-- Footer Submit Actions -->
        <div class="flex justify-end gap-16" *ngIf="!isLocked()">
          <button type="button" class="btn btn-outline" routerLink="/inventory-receipts">Cancel</button>
          <button type="submit" class="btn btn-primary" [disabled]="receiptForm.invalid || submitting() || lines().length === 0">
            {{ submitting() ? 'Saving...' : 'Save Draft' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .receipt-form-container {
      width: 100%;
    }
    .grid { display: grid; }
    .grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .grid-cols-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .gap-24 { gap: 24px; }
    .gap-16 { gap: 16px; }
    .gap-8 { gap: 8px; }
    .mb-24 { margin-bottom: 24px; }
    .mb-4 { margin-bottom: 4px; }
    .mt-16 { margin-top: 16px; }
    .pl-24 { padding-left: 24px; }
    .col-span-2 { grid-column: span 2 / span 2; }
    .col-span-1 { grid-column: span 1 / span 1; }
    
    .border-left {
      border-left: 1px solid var(--border);
    }
    .border-bottom {
      border-bottom: 1px solid var(--border);
    }
    .p-20 { padding: 20px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    
    .flex { display: flex; }
    .flex-col { flex-direction: column; }
    .justify-between { justify-content: space-between; }
    .justify-end { justify-content: flex-end; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .flex-wrap { flex-wrap: wrap; }
    
    .back-link {
      font-size: 0.85rem;
      color: var(--text-muted);
      font-weight: 500;
    }
    .back-link:hover {
      color: var(--primary);
    }
    .back-link span {
      font-size: 16px;
    }
    
    .table-card {
      padding: 0;
      overflow: visible; /* Autocomplete needs dropdown to overlay */
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
      padding: 12px 16px;
      border-bottom: 1px solid var(--border);
      vertical-align: middle;
    }
    .data-table th {
      background-color: #fafbfc;
      font-weight: 600;
      color: var(--text-muted);
      font-size: 0.8rem;
      text-transform: uppercase;
    }
    
    .search-wrapper {
      position: relative;
    }
    .autocomplete-dropdown {
      position: absolute;
      top: 100%;
      left: 0;
      right: 0;
      background: white;
      border: 1px solid var(--border);
      border-radius: 8px;
      box-shadow: var(--shadow-lg);
      z-index: 100;
      max-height: 200px;
      overflow-y: auto;
    }
    .dropdown-item {
      padding: 8px 12px;
      font-size: 0.85rem;
      cursor: pointer;
      color: var(--text-main);
    }
    .dropdown-item:hover {
      background-color: var(--primary-light);
      color: var(--primary);
    }
    .conversion-helper-text {
      font-size: 0.75rem;
      color: var(--text-muted);
      margin-top: 2px;
    }
    
    .code-badge {
      font-family: monospace;
      font-weight: 700;
      background-color: #f1f5f9;
      color: #334155;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 0.75rem;
    }
    
    .text-right { text-align: right; }
    .text-center { text-align: center; }
    .btn-sm { padding: 6px 12px; }
  `]
})
export class ReceiptForm implements OnInit {
  private readonly receiptService = inject(ReceiptService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly supplierService = inject(SupplierService);
  private readonly productService = inject(ProductService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected type = signal<'Import' | 'Export' | 'Transfer'>('Import');
  protected isEditMode = signal(false);
  protected loading = signal(false);
  protected submitting = signal(false);
  
  // Lookups
  protected warehouses = signal<any[]>([]);
  protected suppliers = signal<any[]>([]);
  protected activeProducts: any[] = []; // In memory active products cache for instant autocomplete search!

  // Form states
  protected receiptId = signal<string | null>(null);
  protected receiptNo = signal('');
  protected rowVersion = signal('');
  protected status = signal<number>(0); // 0 = Draft, 1 = Confirmed, 2 = Cancelled
  
  protected receiptData = {
    warehouseId: '',
    fromWarehouseId: '',
    toWarehouseId: '',
    supplierId: '',
    description: ''
  };

  protected lines = signal<ReceiptLineItem[]>([]);

  ngOnInit() {
    this.route.queryParamMap.subscribe(q => {
      const qType = q.get('type');
      if (qType === 'Import' || qType === 'Export' || qType === 'Transfer') {
        this.type.set(qType);
      }
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode.set(true);
        this.receiptId.set(id);
      }
    });

    this.loadLookupsAndData();
  }

  loadLookupsAndData() {
    this.loading.set(true);
    
    // Fetch active lookups
    this.warehouseService.getAll().subscribe(wh => this.warehouses.set(wh.filter(x => x.isActive)));
    this.supplierService.getAll().subscribe(sup => this.suppliers.set(sup.filter(x => x.isActive)));
    
    // Cache all active products for autocomplete search
    this.productService.searchProducts({ page: 1, pageSize: 1000, status: 'Active' }).subscribe(res => {
      this.activeProducts = res.items;
      
      // If edit mode, load the receipt details now
      if (this.isEditMode()) {
        this.loadReceiptDetails();
      } else {
        this.loading.set(false);
      }
    });
  }

  loadReceiptDetails() {
    const id = this.receiptId()!;
    let req;

    if (this.type() === 'Import') {
      req = this.receiptService.getImportReceipt(id);
    } else if (this.type() === 'Export') {
      req = this.receiptService.getExportReceipt(id);
    } else {
      req = this.receiptService.getTransferReceipt(id);
    }

    req.subscribe({
      next: (data) => {
        this.receiptNo.set(data.receiptNo);
        this.status.set(data.status);
        this.rowVersion.set(data.rowVersion);
        
        // Map fields
        this.receiptData.warehouseId = data.warehouseId || '';
        this.receiptData.fromWarehouseId = data.fromWarehouseId || '';
        this.receiptData.toWarehouseId = data.toWarehouseId || '';
        this.receiptData.supplierId = data.supplierId || '';
        this.receiptData.description = data.description || '';

        // Map lines
        const mappedLines = data.lines.map((l: any) => {
          const line: ReceiptLineItem = {
            productId: l.productId,
            productCode: l.product.productCode,
            productName: l.product.name,
            productVariantId: l.productVariantId,
            quantity: l.quantity,
            unitId: l.unitId,
            unitCode: l.unit.code,
            unitPrice: l.unitPrice || 0,
            amount: l.amount || 0,
            productSearch: `${l.product.productCode} - ${l.product.name}`,
            showSearchDropdown: false,
            searchResults: [],
            variants: [],
            conversions: [],
            baseUnitId: l.product.baseUnitId,
            baseUnitCode: '' // Base unit name will populate
          };

          // Fetch product specific details (conversions & variants) for drop-down mapping
          this.productService.getProduct(l.productId).subscribe(p => {
            line.variants = p.variants;
            line.conversions = p.unitConversions;
            line.baseUnitCode = p.baseUnitName;
          });

          return line;
        });

        this.lines.set(mappedLines);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load receipt details.');
        this.router.navigate(['/inventory-receipts']);
        this.loading.set(false);
      }
    });
  }

  isLocked(): boolean {
    return this.isEditMode() && this.status() !== 0; // Locked if Confirmed (1) or Cancelled (2)
  }

  getStatusText(): string {
    const s = this.status();
    if (s === 0) return 'Draft';
    if (s === 1) return 'Confirmed';
    return 'Cancelled';
  }

  validateWarehouses() {
    if (this.type() === 'Transfer' && this.receiptData.fromWarehouseId && this.receiptData.fromWarehouseId === this.receiptData.toWarehouseId) {
      this.toastService.warning('Source and Destination Warehouses must be different.');
      this.receiptData.toWarehouseId = '';
    }
  }

  grandTotal(): number {
    return this.lines().reduce((sum, line) => sum + line.amount, 0);
  }

  addLineItem() {
    const line: ReceiptLineItem = {
      productId: '',
      productCode: '',
      productName: '',
      productVariantId: null,
      quantity: 1,
      unitId: '',
      unitCode: '',
      unitPrice: 0,
      amount: 0,
      productSearch: '',
      showSearchDropdown: false,
      searchResults: [],
      variants: [],
      conversions: [],
      baseUnitId: '',
      baseUnitCode: ''
    };
    this.lines.update(list => [...list, line]);
  }

  removeLineItem(idx: number) {
    this.lines.update(list => list.filter((_, i) => i !== idx));
  }

  searchProduct(item: ReceiptLineItem) {
    const query = item.productSearch.trim().toLowerCase();
    if (!query) {
      item.searchResults = this.activeProducts.slice(0, 10);
    } else {
      item.searchResults = this.activeProducts.filter(p => 
        p.name.toLowerCase().includes(query) || 
        p.productCode.toLowerCase().includes(query)
      ).slice(0, 10);
    }
    item.showSearchDropdown = true;
  }

  selectProduct(item: ReceiptLineItem, product: any) {
    item.productId = product.id;
    item.productCode = product.productCode;
    item.productName = product.name;
    item.productSearch = `${product.productCode} - ${product.name}`;
    item.showSearchDropdown = false;
    item.productVariantId = null;
    item.unitPrice = product.defaultPrice;
    
    // Fetch conversions & variants details dynamically
    this.productService.getProduct(product.id).subscribe(p => {
      item.variants = p.variants;
      item.conversions = p.unitConversions;
      item.baseUnitId = p.baseUnitId;
      item.baseUnitCode = p.baseUnitName;
      item.unitId = p.baseUnitId; // Mặc định base unit
      item.unitCode = p.baseUnitName;
      
      this.recalculateLine(item);
    });
  }

  onVariantChange(item: ReceiptLineItem) {
    // If variant selected, override price if variant price is specified
    if (item.productVariantId) {
      const variant = item.variants.find(v => v.id === item.productVariantId);
      if (variant && variant.price !== null) {
        item.unitPrice = variant.price;
      }
    } else {
      // Revert to product default price
      const activeProd = this.activeProducts.find(p => p.id === item.productId);
      if (activeProd) {
        item.unitPrice = activeProd.defaultPrice;
      }
    }
    this.recalculateLine(item);
  }

  onUnitChange(item: ReceiptLineItem) {
    if (item.unitId === item.baseUnitId) {
      item.unitCode = item.baseUnitCode;
    } else {
      const conv = item.conversions.find(c => c.fromUnitId === item.unitId);
      if (conv) item.unitCode = conv.fromUnitName;
    }
    this.recalculateLine(item);
  }

  getConversionRate(item: ReceiptLineItem): number {
    if (item.unitId === item.baseUnitId) return 1;
    const conv = item.conversions.find(c => c.fromUnitId === item.unitId);
    return conv ? conv.conversionRate : 0;
  }

  recalculateLine(item: ReceiptLineItem) {
    item.amount = item.quantity * item.unitPrice;
  }

  onSubmit(form: any) {
    if (form.invalid || this.lines().length === 0) return;
    this.submitting.set(true);

    // Build line items payload DTO
    const lineDtos = this.lines().map(l => ({
      productId: l.productId,
      productVariantId: l.productVariantId || null,
      quantity: l.quantity,
      unitId: l.unitId,
      unitPrice: this.type() !== 'Transfer' ? l.unitPrice : 0
    }));

    let body: any = {
      description: this.receiptData.description,
      lines: lineDtos
    };

    if (this.type() === 'Import') {
      body.warehouseId = this.receiptData.warehouseId;
      body.supplierId = this.receiptData.supplierId || null;
    } else if (this.type() === 'Export') {
      body.warehouseId = this.receiptData.warehouseId;
    } else {
      body.fromWarehouseId = this.receiptData.fromWarehouseId;
      body.toWarehouseId = this.receiptData.toWarehouseId;
    }

    if (this.isEditMode()) {
      body.rowVersion = this.rowVersion();
      
      let updateReq;
      if (this.type() === 'Import') updateReq = this.receiptService.updateImportReceipt(this.receiptId()!, body);
      else if (this.type() === 'Export') updateReq = this.receiptService.updateExportReceipt(this.receiptId()!, body);
      else updateReq = this.receiptService.updateTransferReceipt(this.receiptId()!, body);

      updateReq.subscribe({
        next: () => {
          this.toastService.success('Receipt updated successfully.');
          this.router.navigate(['/inventory-receipts']);
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update receipt.');
          this.submitting.set(false);
        }
      });
    } else {
      let createReq;
      if (this.type() === 'Import') createReq = this.receiptService.createImportReceipt(body);
      else if (this.type() === 'Export') createReq = this.receiptService.createExportReceipt(body);
      else createReq = this.receiptService.createTransferReceipt(body);

      createReq.subscribe({
        next: () => {
          this.toastService.success('Receipt created as draft.');
          this.router.navigate(['/inventory-receipts']);
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create receipt.');
          this.submitting.set(false);
        }
      });
    }
  }

  onConfirm() {
    if (confirm('Are you sure you want to CONFIRM this receipt? This will adjust stock levels permanently.')) {
      this.loading.set(true);
      let req;

      if (this.type() === 'Import') req = this.receiptService.confirmImportReceipt(this.receiptId()!);
      else if (this.type() === 'Export') req = this.receiptService.confirmExportReceipt(this.receiptId()!);
      else req = this.receiptService.confirmTransferReceipt(this.receiptId()!);

      req.subscribe({
        next: () => {
          this.toastService.success('Receipt confirmed successfully. Stock balances updated.');
          this.loadReceiptDetails();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to confirm receipt. Make sure there is enough stock.');
          this.loading.set(false);
        }
      });
    }
  }

  onCancel() {
    if (confirm('Are you sure you want to CANCEL this receipt? This will revert any draft details.')) {
      this.loading.set(true);
      let req;

      if (this.type() === 'Import') req = this.receiptService.cancelImportReceipt(this.receiptId()!);
      else if (this.type() === 'Export') req = this.receiptService.cancelExportReceipt(this.receiptId()!);
      else req = this.receiptService.cancelTransferReceipt(this.receiptId()!);

      req.subscribe({
        next: () => {
          this.toastService.success('Receipt cancelled successfully.');
          this.loadReceiptDetails();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to cancel receipt.');
          this.loading.set(false);
        }
      });
    }
  }
}

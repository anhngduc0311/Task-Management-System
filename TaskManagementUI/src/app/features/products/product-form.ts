import { Component, OnInit, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { UnitService } from '../../core/services/unit.service';
import { ProductCategoryService } from '../../core/services/product-category.service';
import { OriginService } from '../../core/services/origin.service';
import { SupplierService } from '../../core/services/supplier.service';
import { ProductLabelService } from '../../core/services/product-label.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LoadingSpinner],
  template: `
    <div class="product-form-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center">
        <div>
          <a routerLink="/products" class="back-link flex align-center gap-8 mb-4">
            <span class="material-symbols-rounded">arrow_back</span>
            Back to Products
          </a>
          <h2 style="font-size: 1.5rem; font-weight: 700;">
            {{ isEditMode() ? 'Edit Product' : 'Create Product' }}
          </h2>
        </div>
      </div>

      <!-- Tab Buttons (Only show when editing / product exists) -->
      <div class="tabs-header mb-24" *ngIf="productId()">
        <button class="tab-btn" [class.active]="activeTab() === 'general'" (click)="setTab('general')">General Info</button>
        <button class="tab-btn" [class.active]="activeTab() === 'attributes'" (click)="setTab('attributes')">Attributes & Variants</button>
        <button class="tab-btn" [class.active]="activeTab() === 'conversions'" (click)="setTab('conversions')">Conversions</button>
        <button class="tab-btn" [class.active]="activeTab() === 'images'" (click)="setTab('images')">Images</button>
      </div>

      <!-- General Info Tab -->
      <div class="tab-content" [class.hidden]="activeTab() !== 'general'">
        <form #productForm="ngForm" (ngSubmit)="onProductSubmit(productForm)">
          <div class="glass-card grid grid-cols-3 gap-24">
            
            <div class="col-span-2 grid grid-cols-2 gap-16">
              <div class="form-group">
                <label class="form-label" for="prod-code">Product Code</label>
                <input 
                  type="text" 
                  id="prod-code" 
                  name="productCode" 
                  class="form-input" 
                  [(ngModel)]="productData.productCode" 
                  #prodCode="ngModel" 
                  required 
                  maxlength="100"
                  placeholder="e.g. SP-000001"
                  [disabled]="isEditMode()"
                />
                @if (prodCode.invalid && (prodCode.dirty || prodCode.touched)) {
                  <span class="error-text">Product Code is required.</span>
                }
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-name">Product Name</label>
                <input 
                  type="text" 
                  id="prod-name" 
                  name="name" 
                  class="form-input" 
                  [(ngModel)]="productData.name" 
                  #prodName="ngModel" 
                  required 
                  maxlength="200"
                  placeholder="e.g. iPhone 15 Pro"
                />
                @if (prodName.invalid && (prodName.dirty || prodName.touched)) {
                  <span class="error-text">Product Name is required.</span>
                }
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-price">Default Price</label>
                <input 
                  type="number" 
                  id="prod-price" 
                  name="defaultPrice" 
                  class="form-input" 
                  [(ngModel)]="productData.defaultPrice" 
                  #prodPrice="ngModel"
                  required
                  min="0"
                  placeholder="0.00"
                />
                @if (prodPrice.invalid && (prodPrice.dirty || prodPrice.touched)) {
                  <span class="error-text">Price must be 0 or greater.</span>
                }
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-unit">Base Unit</label>
                <select id="prod-unit" name="baseUnitId" class="form-select" [(ngModel)]="productData.baseUnitId" required [disabled]="isEditMode()">
                  <option [value]="''" disabled>-- Select Base Unit --</option>
                  @for (unit of units(); track unit.id) {
                    <option [value]="unit.id">{{ unit.code }} - {{ unit.name }}</option>
                  }
                </select>
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-cat">Category</label>
                <select id="prod-cat" name="categoryId" class="form-select" [(ngModel)]="productData.categoryId">
                  <option [value]="null">-- None --</option>
                  @for (cat of categories(); track cat.id) {
                    <option [value]="cat.id">{{ cat.code }} - {{ cat.name }}</option>
                  }
                </select>
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-origin">Origin</label>
                <select id="prod-origin" name="originId" class="form-select" [(ngModel)]="productData.originId">
                  <option [value]="null">-- None --</option>
                  @for (org of origins(); track org.id) {
                    <option [value]="org.id">{{ org.code }} - {{ org.name }}</option>
                  }
                </select>
              </div>

              <div class="form-group">
                <label class="form-label" for="prod-status">Status</label>
                <select id="prod-status" name="status" class="form-select" [(ngModel)]="productData.status" required>
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                  <option value="Draft">Draft</option>
                </select>
              </div>
            </div>

            <!-- Description (Rich text contenteditable) & Suppliers & Labels side menu -->
            <div class="col-span-1 border-left pl-24">
              <!-- Associated Labels -->
              <div class="form-group">
                <label class="form-label">Product Labels</label>
                <div class="checkbox-list-container">
                  @for (lbl of labels(); track lbl.id) {
                    <label class="flex align-center gap-8 mb-6 cursor-pointer">
                      <input 
                        type="checkbox" 
                        [checked]="isLabelSelected(lbl.id)"
                        (change)="toggleLabelSelection(lbl.id)"
                      />
                      <span class="chip-label-preview" [style.background-color]="lbl.color + '15'" [style.color]="lbl.color">
                        {{ lbl.name }}
                      </span>
                    </label>
                  }
                </div>
              </div>

              <!-- Associated Suppliers -->
              <div class="form-group">
                <label class="form-label">Suppliers</label>
                <div class="checkbox-list-container">
                  @for (sup of suppliers(); track sup.id) {
                    <label class="flex align-center gap-8 mb-6 cursor-pointer">
                      <input 
                        type="checkbox" 
                        [checked]="isSupplierSelected(sup.id)"
                        (change)="toggleSupplierSelection(sup.id)"
                      />
                      <span>{{ sup.name }}</span>
                    </label>
                  }
                </div>
              </div>
            </div>

            <!-- Description - Full row -->
            <div class="col-span-3">
              <label class="form-label">Description (Rich Text)</label>
              <div class="rich-editor">
                <div class="editor-toolbar">
                  <button type="button" class="btn btn-outline btn-sm font-bold" (click)="execEditorCommand('bold')">B</button>
                  <button type="button" class="btn btn-outline btn-sm font-italic" (click)="execEditorCommand('italic')">I</button>
                  <button type="button" class="btn btn-outline btn-sm" (click)="execEditorCommand('insertUnorderedList')">
                    <span class="material-symbols-rounded" style="font-size: 16px;">format_list_bulleted</span>
                  </button>
                  <button type="button" class="btn btn-outline btn-sm" (click)="execEditorCommand('insertOrderedList')">
                    <span class="material-symbols-rounded" style="font-size: 16px;">format_list_numbered</span>
                  </button>
                  <button type="button" class="btn btn-outline btn-sm" (click)="execEditorCommand('removeFormat')">Clear</button>
                </div>
                <div 
                  #editorContent 
                  class="editor-body" 
                  contenteditable="true" 
                  (blur)="updateDescriptionFromEditor()"
                  placeholder="Describe your product here..."
                ></div>
              </div>
            </div>

            <!-- Attribute groups setup (Only during create mode) -->
            <div class="col-span-3 border-top mt-16 pt-16" *ngIf="!isEditMode()">
              <h3 style="font-size: 1.1rem; font-weight: 600;" class="mb-12">Product Attribute Groups (Max 2, e.g. Color, Size)</h3>
              
              <div class="grid grid-cols-2 gap-24">
                <!-- Group 1 -->
                <div class="glass-card group-card">
                  <div class="form-group">
                    <label class="form-label">Group 1 Name (e.g. Size)</label>
                    <input type="text" class="form-input" name="group1Name" [(ngModel)]="creationAttributes.group1.name" placeholder="e.g. Size" />
                  </div>
                  <div class="form-group">
                    <label class="form-label">Values (Comma separated, e.g. S, M, L)</label>
                    <input type="text" class="form-input" name="group1Values" [(ngModel)]="creationAttributes.group1.valuesStr" placeholder="e.g. S, M, L" />
                  </div>
                </div>

                <!-- Group 2 -->
                <div class="glass-card group-card">
                  <div class="form-group">
                    <label class="form-label">Group 2 Name (e.g. Color)</label>
                    <input type="text" class="form-input" name="group2Name" [(ngModel)]="creationAttributes.group2.name" placeholder="e.g. Color" />
                  </div>
                  <div class="form-group">
                    <label class="form-label">Values (Comma separated, e.g. Black, White)</label>
                    <input type="text" class="form-input" name="group2Values" [(ngModel)]="creationAttributes.group2.valuesStr" placeholder="e.g. Black, White" />
                  </div>
                </div>
              </div>
            </div>

            <div class="col-span-3 flex justify-end gap-16 border-top mt-16 pt-16">
              <button type="button" class="btn btn-outline" routerLink="/products">Cancel</button>
              <button type="submit" class="btn btn-primary" [disabled]="productForm.invalid || submitting()">
                {{ submitting() ? 'Saving...' : (isEditMode() ? 'Update Product' : 'Create Product') }}
              </button>
            </div>
          </div>
        </form>
      </div>

      <!-- Attributes & Variants Tab -->
      <div class="tab-content" [class.hidden]="activeTab() !== 'attributes'" *ngIf="productId()">
        <div class="glass-card">
          <div class="flex justify-between align-center mb-16">
            <h3 style="font-size: 1.2rem; font-weight: 600;">Product Variants</h3>
          </div>

          <!-- Attributes Display -->
          <div class="flex gap-16 mb-24 flex-wrap">
            @for (g of fullProductDetails.attributeGroups; track g.id) {
              <div class="attribute-group-badge">
                <span class="g-name">{{ g.name }}:</span>
                <span class="g-values">{{ getGroupValuesStr(g) }}</span>
              </div>
            }
          </div>

          <!-- Variants Table -->
          <table class="data-table mb-24">
            <thead>
              <tr>
                <th>Combination</th>
                <th>SKU Code</th>
                <th>Price</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <!-- Active variants -->
              @for (v of fullProductDetails.variants; track v.id) {
                <tr>
                  <td><strong>{{ v.attributeValueCombinations }}</strong></td>
                  <td>
                    <span class="code-badge">{{ v.sku }}</span>
                  </td>
                  <td>{{ v.price ? (v.price | currency:'USD':'symbol':'1.2-2') : 'Use Default' }}</td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-text btn-sm" (click)="openEditVariantModal(v)" title="Edit Price/SKU">
                      <span class="material-symbols-rounded text-primary">edit</span>
                    </button>
                    <button class="btn btn-text btn-sm" (click)="onDeleteVariant(v.id)" title="Delete Variant">
                      <span class="material-symbols-rounded text-danger">delete</span>
                    </button>
                  </td>
                </tr>
              }

              <!-- Cartesian Product missing combinations generator -->
              @for (comb of getMissingCombinations(); track comb.combinationText) {
                <tr class="missing-variant-row">
                  <td><span class="missing-badge">{{ comb.combinationText }}</span></td>
                  <td>
                    <input 
                      type="text" 
                      class="form-input form-input-sm" 
                      placeholder="Enter unique SKU" 
                      [(ngModel)]="comb.sku"
                    />
                  </td>
                  <td>
                    <input 
                      type="number" 
                      class="form-input form-input-sm" 
                      placeholder="Price (Optional)" 
                      [(ngModel)]="comb.price"
                    />
                  </td>
                  <td class="text-right actions-cell">
                    <button class="btn btn-primary btn-sm" [disabled]="!comb.sku" (click)="createVariantFromCombination(comb)">
                      Add Variant
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Unit Conversions Tab -->
      <div class="tab-content" [class.hidden]="activeTab() !== 'conversions'" *ngIf="productId()">
        <div class="glass-card">
          <div class="flex justify-between align-center mb-16">
            <h3 style="font-size: 1.2rem; font-weight: 600;">Unit Conversions</h3>
            <button class="btn btn-primary btn-sm" (click)="openCreateConversionModal()">
              <span class="material-symbols-rounded" style="font-size: 16px;">add</span>
              Add Conversion
            </button>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Conversion rule</th>
                <th>Rate (to base unit)</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (fullProductDetails.unitConversions.length === 0) {
                <tr>
                  <td colspan="3" class="text-center text-muted py-24">No conversions configured.</td>
                </tr>
              } @else {
                @for (c of fullProductDetails.unitConversions; track c.id) {
                  <tr>
                    <td>
                      1 <strong>{{ c.fromUnitName }}</strong> = {{ c.conversionRate }} <strong>{{ c.toUnitName }}</strong>
                    </td>
                    <td>{{ c.conversionRate }}</td>
                    <td class="text-right actions-cell">
                      <button class="btn btn-text btn-sm" (click)="openEditConversionModal(c)" title="Edit">
                        <span class="material-symbols-rounded text-primary">edit</span>
                      </button>
                      <button class="btn btn-text btn-sm" (click)="onDeleteConversion(c.id)" title="Delete">
                        <span class="material-symbols-rounded text-danger">delete</span>
                      </button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Images Tab -->
      <div class="tab-content" [class.hidden]="activeTab() !== 'images'" *ngIf="productId()">
        <div class="glass-card">
          <div class="flex justify-between align-center mb-16">
            <h3 style="font-size: 1.2rem; font-weight: 600;">Product Images (Max 10)</h3>
            
            <div class="upload-actions">
              <input type="file" #fileInput multiple accept="image/*" class="hidden" (change)="onFileSelected($event)" />
              <button class="btn btn-primary btn-sm" (click)="fileInput.click()" [disabled]="fullProductDetails.images.length >= 10">
                <span class="material-symbols-rounded" style="font-size: 16px;">upload</span>
                Upload Images
              </button>
            </div>
          </div>

          <div class="image-gallery grid grid-cols-5 gap-16">
            @for (img of fullProductDetails.images; track img.id) {
              <div class="image-card" [class.primary]="img.isPrimary">
                <div class="img-wrapper">
                  <img [src]="'http://localhost:5035' + img.url" [alt]="img.fileName" />
                </div>
                <div class="image-info">
                  <span class="name">{{ img.fileName }}</span>
                </div>
                <div class="image-overlay flex justify-center align-center gap-8">
                  @if (!img.isPrimary) {
                    <button class="btn btn-secondary btn-sm" (click)="setPrimaryImage(img.id)">Set Primary</button>
                  } @else {
                    <span class="primary-indicator">Primary</span>
                  }
                  <button class="btn btn-danger btn-sm" style="padding: 6px;" (click)="onDeleteImage(img.id)">
                    <span class="material-symbols-rounded" style="font-size: 16px;">delete</span>
                  </button>
                </div>
              </div>
            }
          </div>
        </div>
      </div>

      <!-- Modal Dialogs for child operations -->
      
      <!-- Variant Modal -->
      @if (showVariantModal()) {
        <div class="modal-overlay" (click)="closeVariantModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Edit Variant</h3>
              <button class="close-btn" (click)="closeVariantModal()">&times;</button>
            </div>
            <div class="modal-body">
              <div class="form-group">
                <label class="form-label">Combination</label>
                <input type="text" class="form-input" [value]="currentVariant.combinationText" disabled />
              </div>
              <div class="form-group">
                <label class="form-label">SKU</label>
                <input type="text" class="form-input" [(ngModel)]="currentVariant.sku" required />
              </div>
              <div class="form-group">
                <label class="form-label">Price Override (Optional)</label>
                <input type="number" class="form-input" [(ngModel)]="currentVariant.price" placeholder="Price in USD" />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline" (click)="closeVariantModal()">Cancel</button>
              <button type="button" class="btn btn-primary" (click)="saveVariant()">Save</button>
            </div>
          </div>
        </div>
      }

      <!-- Conversion Modal -->
      @if (showConversionModal()) {
        <div class="modal-overlay" (click)="closeConversionModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ isEditConversionMode() ? 'Edit Conversion' : 'Add Unit Conversion' }}</h3>
              <button class="close-btn" (click)="closeConversionModal()">&times;</button>
            </div>
            <div class="modal-body">
              <div class="form-group">
                <label class="form-label" for="conv-from">From Unit</label>
                <select id="conv-from" class="form-select" [(ngModel)]="currentConversion.fromUnitId" [disabled]="isEditConversionMode()">
                  @for (unit of units(); track unit.id) {
                    <option [value]="unit.id">{{ unit.code }} - {{ unit.name }}</option>
                  }
                </select>
              </div>
              <div class="form-group">
                <label class="form-label" for="conv-to">To Unit (Base Unit)</label>
                <select id="conv-to" class="form-select" [value]="productData.baseUnitId" disabled>
                  @for (unit of units(); track unit.id) {
                    @if (unit.id === productData.baseUnitId) {
                      <option [value]="unit.id">{{ unit.code }} - {{ unit.name }}</option>
                    }
                  }
                </select>
              </div>
              <div class="form-group">
                <label class="form-label" for="conv-rate">Conversion Rate (From → To)</label>
                <input type="number" id="conv-rate" class="form-input" [(ngModel)]="currentConversion.conversionRate" placeholder="e.g. 12" />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline" (click)="closeConversionModal()">Cancel</button>
              <button type="button" class="btn btn-primary" (click)="saveConversion()">Save</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .product-form-container {
      width: 100%;
    }
    .grid { display: grid; }
    .grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .grid-cols-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .grid-cols-5 { grid-template-columns: repeat(5, minmax(0, 1fr)); }
    .gap-24 { gap: 24px; }
    .gap-16 { gap: 16px; }
    .gap-12 { gap: 12px; }
    .gap-8 { gap: 8px; }
    .mb-24 { margin-bottom: 24px; }
    .mb-12 { margin-bottom: 12px; }
    .mb-4 { margin-bottom: 4px; }
    .mb-6 { margin-bottom: 6px; }
    .mt-16 { margin-top: 16px; }
    .pt-16 { padding-top: 16px; }
    .pl-24 { padding-left: 24px; }
    .col-span-2 { grid-column: span 2 / span 2; }
    .col-span-3 { grid-column: span 3 / span 3; }
    .border-left {
      border-left: 1px solid var(--border);
    }
    .border-top {
      border-top: 1px solid var(--border);
    }
    .flex { display: flex; }
    .justify-between { justify-content: space-between; }
    .justify-end { justify-content: flex-end; }
    .justify-center { justify-content: center; }
    .align-center { align-items: center; }
    .align-end { align-items: flex-end; }
    .pb-8 { padding-bottom: 8px; }
    .py-8 { padding-top: 8px; padding-bottom: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
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
    
    /* Tabs styling */
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
    .tab-btn:hover:not([disabled]) {
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
    .tab-btn[disabled] {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .hidden {
      display: none !important;
    }

    .checkbox-list-container {
      max-height: 140px;
      overflow-y: auto;
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 8px 12px;
      background-color: var(--bg-input);
    }
    
    .chip-label-preview {
      font-size: 0.75rem;
      font-weight: 600;
      padding: 2px 6px;
      border-radius: 4px;
    }
    
    /* Rich Editor Styling */
    .rich-editor {
      border: 1px solid var(--border);
      border-radius: 8px;
      overflow: hidden;
      background-color: var(--bg-input);
      margin-top: 6px;
    }
    .editor-toolbar {
      background-color: #fafbfc;
      border-bottom: 1px solid var(--border);
      padding: 6px;
      display: flex;
      gap: 6px;
    }
    .editor-toolbar button {
      padding: 4px 10px;
      font-size: 0.85rem;
      min-width: 32px;
    }
    .editor-body {
      min-height: 160px;
      padding: 12px 16px;
      outline: none;
      overflow-y: auto;
      color: var(--text-main);
      font-size: 0.95rem;
      line-height: 1.5;
    }
    
    .attribute-group-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background-color: var(--primary-light);
      color: var(--primary);
      padding: 6px 12px;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
    }
    .attribute-group-badge .g-name {
      font-weight: 700;
    }
    .attribute-group-badge .g-values {
      color: var(--text-main);
    }
    
    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    .data-table th, .data-table td {
      padding: 12px 16px;
      border-bottom: 1px solid var(--border);
    }
    .data-table th {
      background-color: #fafbfc;
      font-weight: 600;
      color: var(--text-muted);
      font-size: 0.8rem;
      text-transform: uppercase;
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
    .missing-variant-row {
      background-color: rgba(99, 102, 241, 0.03);
    }
    .missing-badge {
      font-weight: 600;
      color: var(--text-muted);
      font-size: 0.9rem;
    }
    .form-input-sm {
      padding: 6px 10px;
      font-size: 0.85rem;
      max-width: 160px;
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
    .text-center {
      text-align: center;
    }
    .text-right {
      text-align: right;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: var(--text-light);
    }
    .error-text {
      font-size: 0.75rem;
      color: var(--danger);
      margin-top: 4px;
      display: block;
    }
    
    /* Image gallery */
    .image-card {
      position: relative;
      border: 1px solid var(--border);
      border-radius: 12px;
      overflow: hidden;
      aspect-ratio: 1;
      display: flex;
      flex-direction: column;
      background: white;
      transition: all var(--transition-normal);
    }
    .image-card.primary {
      border-color: var(--primary);
      box-shadow: 0 0 0 2px var(--primary-light);
    }
    .image-card .img-wrapper {
      flex-grow: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
      background: #fafbfc;
    }
    .image-card img {
      max-width: 100%;
      max-height: 100%;
      object-fit: cover;
    }
    .image-card .image-info {
      padding: 8px 12px;
      font-size: 0.75rem;
      color: var(--text-muted);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      border-top: 1px solid var(--border);
    }
    .image-card .image-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(15, 23, 42, 0.6);
      opacity: 0;
      transition: opacity var(--transition-fast);
    }
    .image-card:hover .image-overlay {
      opacity: 1;
    }
    .primary-indicator {
      background-color: var(--primary);
      color: white;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
    }
    .hidden {
      display: none;
    }
  `]
})
export class ProductForm implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly unitService = inject(UnitService);
  private readonly categoryService = inject(ProductCategoryService);
  private readonly originService = inject(OriginService);
  private readonly supplierService = inject(SupplierService);
  private readonly labelService = inject(ProductLabelService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  @ViewChild('editorContent') editorContent!: ElementRef<HTMLDivElement>;

  protected activeTab = signal<'general' | 'attributes' | 'conversions' | 'images'>('general');
  protected loading = signal(false);
  protected submitting = signal(false);
  protected isEditMode = signal(false);
  protected productId = signal<string | null>(null);

  // Lists
  protected units = signal<any[]>([]);
  protected categories = signal<any[]>([]);
  protected origins = signal<any[]>([]);
  protected suppliers = signal<any[]>([]);
  protected labels = signal<any[]>([]);

  // Form inputs
  protected productData = {
    productCode: '',
    name: '',
    description: '',
    defaultPrice: 0,
    baseUnitId: '',
    categoryId: null as string | null,
    originId: null as string | null,
    status: 'Active',
    supplierIds: [] as string[],
    labelIds: [] as string[],
    rowVersion: ''
  };

  // Attributes for new products
  protected creationAttributes = {
    group1: { name: '', valuesStr: '' },
    group2: { name: '', valuesStr: '' }
  };

  // Full product details fetched for variants/conversions/images tabs
  protected fullProductDetails: any = {
    attributeGroups: [],
    variants: [],
    unitConversions: [],
    images: []
  };

  // Child Modals State
  protected showVariantModal = signal(false);
  protected currentVariant = { id: '', combinationText: '', sku: '', price: null as number | null };

  protected showConversionModal = signal(false);
  protected isEditConversionMode = signal(false);
  protected currentConversion = { id: '', fromUnitId: '', toUnitId: '', conversionRate: 1 };

  ngOnInit() {
    this.loadLookups();

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode.set(true);
        this.productId.set(id);
        this.loadProductDetails(id);
      }
    });
  }

  loadLookups() {
    this.unitService.getAll().subscribe(data => this.units.set(data.filter(x => x.isActive)));
    this.categoryService.getTree().subscribe(data => {
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

  loadProductDetails(id: string) {
    this.loading.set(true);
    this.productService.getProduct(id).subscribe({
      next: (data) => {
        this.fullProductDetails = data;
        
        // Map basic data
        this.productData.productCode = data.productCode;
        this.productData.name = data.name;
        this.productData.description = data.description || '';
        this.productData.defaultPrice = data.defaultPrice;
        this.productData.baseUnitId = data.baseUnitId;
        this.productData.categoryId = data.categoryId;
        this.productData.originId = data.originId;
        this.productData.status = data.status;
        this.productData.rowVersion = data.rowVersion;
        
        // Map associated
        this.productData.supplierIds = data.suppliers.map((s: any) => s.id);
        this.productData.labelIds = data.labels.map((l: any) => l.id);

        // Update editor body if loaded
        if (this.editorContent?.nativeElement) {
          this.editorContent.nativeElement.innerHTML = data.description || '';
        }

        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load product details.');
        this.router.navigate(['/products']);
        this.loading.set(false);
      }
    });
  }

  setTab(tab: 'general' | 'attributes' | 'conversions' | 'images') {
    this.activeTab.set(tab);
    if (tab === 'general' && this.editorContent?.nativeElement) {
      setTimeout(() => {
        this.editorContent.nativeElement.innerHTML = this.productData.description || '';
      }, 0);
    }
  }

  execEditorCommand(command: string) {
    document.execCommand(command, false, '');
    this.updateDescriptionFromEditor();
  }

  updateDescriptionFromEditor() {
    if (this.editorContent?.nativeElement) {
      this.productData.description = this.editorContent.nativeElement.innerHTML;
    }
  }

  // Label checking helpers
  isLabelSelected(id: string): boolean {
    return this.productData.labelIds.includes(id);
  }

  toggleLabelSelection(id: string) {
    const list = [...this.productData.labelIds];
    const index = list.indexOf(id);
    if (index > -1) {
      list.splice(index, 1);
    } else {
      list.push(id);
    }
    this.productData.labelIds = list;
  }

  // Supplier checking helpers
  isSupplierSelected(id: string): boolean {
    return this.productData.supplierIds.includes(id);
  }

  toggleSupplierSelection(id: string) {
    const list = [...this.productData.supplierIds];
    const index = list.indexOf(id);
    if (index > -1) {
      list.splice(index, 1);
    } else {
      list.push(id);
    }
    this.productData.supplierIds = list;
  }

  onProductSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    this.updateDescriptionFromEditor();

    // Map body
    const body: any = {
      productCode: this.productData.productCode,
      name: this.productData.name,
      description: this.productData.description,
      defaultPrice: this.productData.defaultPrice,
      baseUnitId: this.productData.baseUnitId,
      categoryId: this.productData.categoryId || null,
      originId: this.productData.originId || null,
      status: this.productData.status,
      supplierIds: this.productData.supplierIds,
      labelIds: this.productData.labelIds
    };

    if (this.isEditMode()) {
      body.rowVersion = this.productData.rowVersion;
      this.productService.updateProduct(this.productId()!, body).subscribe({
        next: () => {
          this.toastService.success('Product updated successfully.');
          this.loadProductDetails(this.productId()!);
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update product (concurrency conflict could have occurred).');
          this.submitting.set(false);
        }
      });
    } else {
      // Build attributeGroups DTO if entered
      const attributeGroups: any[] = [];
      if (this.creationAttributes.group1.name && this.creationAttributes.group1.valuesStr) {
        attributeGroups.push({
          name: this.creationAttributes.group1.name.trim(),
          displayOrder: 0,
          values: this.creationAttributes.group1.valuesStr.split(',').map(x => x.trim()).filter(x => x)
        });
      }
      if (this.creationAttributes.group2.name && this.creationAttributes.group2.valuesStr) {
        attributeGroups.push({
          name: this.creationAttributes.group2.name.trim(),
          displayOrder: 1,
          values: this.creationAttributes.group2.valuesStr.split(',').map(x => x.trim()).filter(x => x)
        });
      }

      body.attributeGroups = attributeGroups;

      this.productService.createProduct(body).subscribe({
        next: (res) => {
          this.toastService.success('Product created successfully. You can now configure variants, conversions, and upload images.');
          this.isEditMode.set(true);
          this.productId.set(res.id);
          this.loadProductDetails(res.id);
          this.setTab('attributes');
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create product.');
          this.submitting.set(false);
        }
      });
    }
  }

  /* ==============================================================
     VARIANTS TAB & CARTESIAN PRODUCT CALCULATIONS
     ============================================================== */
  getGroupValuesStr(g: any): string {
    return g.attributeValues.map((v: any) => v.value).join(', ');
  }

  getMissingCombinations(): any[] {
    const groups = this.fullProductDetails.attributeGroups;
    if (!groups || groups.length === 0) return [];

    // Cartesian combinations
    let combos: { values: any[]; combinationText: string; ids: string[] }[] = [];

    // Generate Cartesian
    if (groups.length === 1) {
      combos = groups[0].attributeValues.map((val: any) => ({
        values: [val.value],
        combinationText: val.value,
        ids: [val.id]
      }));
    } else if (groups.length === 2) {
      for (const val1 of groups[0].attributeValues) {
        for (const val2 of groups[1].attributeValues) {
          combos.push({
            values: [val1.value, val2.value],
            combinationText: `${val1.value} / ${val2.value}`,
            ids: [val1.id, val2.id]
          });
        }
      }
    }

    // Filter out existing variants
    const activeVariants = this.fullProductDetails.variants || [];
    return combos
      .filter(comb => {
        // Find if this combo exists in activeVariants
        const exists = activeVariants.some((v: any) => 
          v.attributeValueIds.length === comb.ids.length &&
          v.attributeValueIds.every((id: string) => comb.ids.includes(id))
        );
        return !exists;
      })
      .map(comb => ({
        combinationText: comb.combinationText,
        attributeValueIds: comb.ids,
        sku: `${this.productData.productCode}-${comb.values.join('-').toUpperCase().replace(/\\s+/g, '')}`,
        price: this.productData.defaultPrice
      }));
  }

  createVariantFromCombination(comb: any) {
    this.loading.set(true);
    const body = {
      sku: comb.sku,
      price: comb.price || null,
      imageUrl: null,
      attributeValueIds: comb.attributeValueIds
    };

    this.productService.createVariant(this.productId()!, body).subscribe({
      next: () => {
        this.toastService.success('Variant created successfully.');
        this.loadProductDetails(this.productId()!);
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'Failed to create variant.');
        this.loading.set(false);
      }
    });
  }

  openEditVariantModal(variant: any) {
    this.currentVariant = {
      id: variant.id,
      combinationText: variant.attributeValueCombinations,
      sku: variant.sku,
      price: variant.price
    };
    this.showVariantModal.set(true);
  }

  closeVariantModal() {
    this.showVariantModal.set(false);
  }

  saveVariant() {
    if (!this.currentVariant.sku) return;
    this.loading.set(true);
    
    this.productService.updateVariant(this.productId()!, this.currentVariant.id, {
      sku: this.currentVariant.sku,
      price: this.currentVariant.price
    }).subscribe({
      next: () => {
        this.toastService.success('Variant updated successfully.');
        this.closeVariantModal();
        this.loadProductDetails(this.productId()!);
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'Failed to update variant.');
        this.loading.set(false);
      }
    });
  }

  onDeleteVariant(variantId: string) {
    if (confirm('Are you sure you want to delete this variant?')) {
      this.loading.set(true);
      this.productService.deleteVariant(this.productId()!, variantId).subscribe({
        next: () => {
          this.toastService.success('Variant deleted successfully.');
          this.loadProductDetails(this.productId()!);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete variant.');
          this.loading.set(false);
        }
      });
    }
  }

  /* ==============================================================
     UNIT CONVERSIONS TAB
     ============================================================== */
  openCreateConversionModal() {
    this.isEditConversionMode.set(false);
    this.currentConversion = {
      id: '',
      fromUnitId: '',
      toUnitId: this.productData.baseUnitId,
      conversionRate: 1
    };
    this.showConversionModal.set(true);
  }

  openEditConversionModal(conv: any) {
    this.isEditConversionMode.set(true);
    this.currentConversion = {
      id: conv.id,
      fromUnitId: conv.fromUnitId,
      toUnitId: conv.toUnitId,
      conversionRate: conv.conversionRate
    };
    this.showConversionModal.set(true);
  }

  closeConversionModal() {
    this.showConversionModal.set(false);
  }

  saveConversion() {
    if (!this.currentConversion.fromUnitId || this.currentConversion.conversionRate <= 0) {
      this.toastService.warning('Please input a valid unit and rate greater than 0.');
      return;
    }
    this.loading.set(true);

    const body = {
      fromUnitId: this.currentConversion.fromUnitId,
      toUnitId: this.productData.baseUnitId,
      conversionRate: this.currentConversion.conversionRate
    };

    if (this.isEditConversionMode()) {
      this.productService.updateConversion(this.productId()!, this.currentConversion.id, body).subscribe({
        next: () => {
          this.toastService.success('Conversion updated successfully.');
          this.closeConversionModal();
          this.loadProductDetails(this.productId()!);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update conversion.');
          this.loading.set(false);
        }
      });
    } else {
      this.productService.createConversion(this.productId()!, body).subscribe({
        next: () => {
          this.toastService.success('Conversion added successfully.');
          this.closeConversionModal();
          this.loadProductDetails(this.productId()!);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to add conversion.');
          this.loading.set(false);
        }
      });
    }
  }

  onDeleteConversion(convId: string) {
    if (confirm('Are you sure you want to delete this unit conversion?')) {
      this.loading.set(true);
      this.productService.deleteConversion(this.productId()!, convId).subscribe({
        next: () => {
          this.toastService.success('Conversion deleted successfully.');
          this.loadProductDetails(this.productId()!);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete conversion.');
          this.loading.set(false);
        }
      });
    }
  }

  /* ==============================================================
     IMAGES TAB
     ============================================================== */
  onFileSelected(event: any) {
    const filesList: FileList = event.target.files;
    if (!filesList || filesList.length === 0) return;

    const files: File[] = [];
    for (let i = 0; i < filesList.length; i++) {
      files.push(filesList[i]);
    }

    if (this.fullProductDetails.images.length + files.length > 10) {
      this.toastService.warning('A product can have a maximum of 10 images.');
      return;
    }

    this.loading.set(true);
    this.productService.uploadImages(this.productId()!, files).subscribe({
      next: () => {
        this.toastService.success('Images uploaded successfully.');
        this.loadProductDetails(this.productId()!);
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'Failed to upload images.');
        this.loading.set(false);
      }
    });
  }

  setPrimaryImage(imageId: string) {
    this.loading.set(true);
    this.productService.setPrimaryImage(this.productId()!, imageId).subscribe({
      next: () => {
        this.toastService.success('Primary image updated.');
        this.loadProductDetails(this.productId()!);
      },
      error: () => {
        this.toastService.error('Failed to update primary image.');
        this.loading.set(false);
      }
    });
  }

  onDeleteImage(imageId: string) {
    if (confirm('Are you sure you want to delete this image?')) {
      this.loading.set(true);
      this.productService.deleteImage(this.productId()!, imageId).subscribe({
        next: () => {
          this.toastService.success('Image deleted successfully.');
          this.loadProductDetails(this.productId()!);
        },
        error: () => {
          this.toastService.error('Failed to delete image.');
          this.loading.set(false);
        }
      });
    }
  }
}

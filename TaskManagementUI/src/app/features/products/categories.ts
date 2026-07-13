import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductCategoryService } from '../../core/services/product-category.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner],
  template: `
    <div class="categories-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search categories..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        <button class="btn btn-primary" (click)="openCreateModal()">
          <span class="material-symbols-rounded">add</span>
          New Root Category
        </button>
      </div>

      <!-- Recursive Tree Container -->
      <div class="glass-card tree-card">
        @if (treeData().length === 0) {
          <div class="text-center text-muted py-24">No categories found.</div>
        } @else {
          <div class="tree-root">
            @for (node of treeData(); track node.id) {
              <ng-container *ngTemplateOutlet="nodeTemplate; context: { $implicit: node, depth: 0 }"></ng-container>
            }
          </div>
        }
      </div>

      <!-- Recursive Template for Tree Node -->
      <ng-template #nodeTemplate let-node let-depth="depth">
        @if (shouldShowNode(node)) {
          <div class="tree-node" [style.padding-left.px]="depth * 20">
            <div class="node-wrapper flex justify-between align-center py-8">
              <div class="flex align-center gap-8">
                <!-- Toggle Expand -->
                @if (node.children && node.children.length > 0) {
                  <button class="toggle-btn" (click)="toggleExpand(node.id)" [class.expanded]="expandedNodes().has(node.id)">
                    <span class="material-symbols-rounded">chevron_right</span>
                  </button>
                } @else {
                  <span class="toggle-placeholder"></span>
                }
                
                <span class="material-symbols-rounded node-icon">folder</span>
                
                <div class="node-info">
                  <span class="node-code">{{ node.code }}</span>
                  <span class="node-name">{{ node.name }}</span>
                  @if (node.description) {
                    <span class="node-desc text-muted">- {{ node.description }}</span>
                  }
                </div>

                <span class="badge" [class.badge-status-todo]="!node.isActive" [class.badge-status-progress]="node.isActive" style="background-color: node.isActive ? 'var(--success-bg)' : 'var(--border)'; color: node.isActive ? 'var(--success)' : 'var(--text-muted)'">
                  {{ node.isActive ? 'Active' : 'Inactive' }}
                </span>
              </div>

              <div class="node-actions flex gap-4">
                <button class="btn btn-text btn-sm" (click)="openCreateChildModal(node)" title="Add Sub-category">
                  <span class="material-symbols-rounded text-primary">add</span>
                </button>
                <button class="btn btn-text btn-sm" (click)="openEditModal(node)" title="Edit">
                  <span class="material-symbols-rounded text-primary">edit</span>
                </button>
                <button class="btn btn-text btn-sm" (click)="onDelete(node.id)" title="Delete">
                  <span class="material-symbols-rounded text-danger">delete</span>
                </button>
              </div>
            </div>

            <!-- Children Rendering -->
            @if (node.children && node.children.length > 0 && expandedNodes().has(node.id)) {
              <div class="node-children">
                @for (child of node.children; track child.id) {
                  <ng-container *ngTemplateOutlet="nodeTemplate; context: { $implicit: child, depth: depth + 1 }"></ng-container>
                }
              </div>
            }
          </div>
        }
      </ng-template>

      <!-- Add/Edit Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ isEditMode() ? 'Edit Category' : 'Create New Category' }}</h3>
              <button class="close-btn" (click)="closeModal()">&times;</button>
            </div>
            
            <form #categoryForm="ngForm" (ngSubmit)="onSubmit(categoryForm)">
              <div class="modal-body">
                @if (currentCategory.parentName) {
                  <div class="form-group">
                    <label class="form-label">Parent Category</label>
                    <input type="text" class="form-input" [value]="currentCategory.parentName" disabled />
                  </div>
                } @else if (!isEditMode()) {
                  <div class="form-group">
                    <label class="form-label" for="cat-parent">Parent Category (Optional)</label>
                    <select id="cat-parent" name="parentId" class="form-select" [(ngModel)]="currentCategory.parentId">
                      <option [value]="null">-- None (Root Category) --</option>
                      @for (flat of flatCategoriesList(); track flat.id) {
                        <option [value]="flat.id">{{ flat.code }} - {{ flat.name }}</option>
                      }
                    </select>
                  </div>
                } @else {
                  <!-- Edit mode parent selection (filter out children to prevent circular reference) -->
                  <div class="form-group">
                    <label class="form-label" for="cat-parent">Parent Category</label>
                    <select id="cat-parent" name="parentId" class="form-select" [(ngModel)]="currentCategory.parentId">
                      <option [value]="null">-- None (Root Category) --</option>
                      @for (flat of getValidParents(); track flat.id) {
                        <option [value]="flat.id">{{ flat.code }} - {{ flat.name }}</option>
                      }
                    </select>
                  </div>
                }

                <div class="form-group">
                  <label class="form-label" for="cat-code">Category Code</label>
                  <input 
                    type="text" 
                    id="cat-code" 
                    name="code" 
                    class="form-input" 
                    [(ngModel)]="currentCategory.code" 
                    #catCode="ngModel" 
                    required 
                    maxlength="50"
                    placeholder="e.g. ELEC, PHONE, COMP"
                    [disabled]="isEditMode()"
                  />
                  @if (catCode.invalid && (catCode.dirty || catCode.touched)) {
                    <span class="error-text">Category code is required.</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="cat-name">Category Name</label>
                  <input 
                    type="text" 
                    id="cat-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="currentCategory.name" 
                    #catName="ngModel" 
                    required 
                    maxlength="100"
                    placeholder="e.g. Electronics, Smart Phones"
                  />
                  @if (catName.invalid && (catName.dirty || catName.touched)) {
                    <span class="error-text">Category name is required.</span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="cat-desc">Description</label>
                  <textarea 
                    id="cat-desc" 
                    name="description" 
                    class="form-input form-textarea" 
                    [(ngModel)]="currentCategory.description" 
                    maxlength="500"
                    placeholder="Describe the category..."
                  ></textarea>
                </div>

                <div class="grid grid-cols-2 gap-16">
                  <div class="form-group">
                    <label class="form-label" for="cat-order">Display Order</label>
                    <input 
                      type="number" 
                      id="cat-order" 
                      name="displayOrder" 
                      class="form-input" 
                      [(ngModel)]="currentCategory.displayOrder" 
                    />
                  </div>
                  <div class="form-group flex align-end pb-8">
                    <div class="flex align-center gap-8">
                      <input 
                        type="checkbox" 
                        id="cat-active" 
                        name="isActive" 
                        [(ngModel)]="currentCategory.isActive"
                      />
                      <label for="cat-active" class="form-label mb-0" style="cursor: pointer;">Active</label>
                    </div>
                  </div>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="categoryForm.invalid || submitting()">
                  {{ submitting() ? 'Saving...' : 'Save Category' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .categories-container {
      width: 100%;
    }
    .search-input {
      min-width: 280px;
    }
    .tree-card {
      padding: 24px;
    }
    .tree-root {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .tree-node {
      display: flex;
      flex-direction: column;
    }
    .node-wrapper {
      border-bottom: 1px solid var(--border);
      transition: background-color var(--transition-fast);
      border-radius: 6px;
      padding: 6px 12px;
    }
    .node-wrapper:hover {
      background-color: rgba(241, 245, 249, 0.5);
    }
    .toggle-btn {
      background: none;
      border: none;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      padding: 0;
      color: var(--text-muted);
      transition: transform var(--transition-fast);
    }
    .toggle-btn.expanded {
      transform: rotate(90deg);
    }
    .toggle-placeholder {
      display: inline-block;
      width: 24px;
    }
    .node-icon {
      color: var(--primary);
      font-size: 20px;
    }
    .node-info {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 0.95rem;
    }
    .node-code {
      font-weight: 700;
      color: var(--primary);
      background-color: var(--primary-light);
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 0.75rem;
    }
    .node-name {
      color: var(--text-main);
      font-weight: 500;
    }
    .node-desc {
      font-size: 0.85rem;
      font-weight: 400;
    }
    .node-children {
      margin-left: 12px;
      border-left: 1px dashed var(--border);
    }
    .btn-sm {
      padding: 4px 8px;
    }
    
    .grid { display: grid; }
    .grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .gap-16 { gap: 16px; }
    .align-end { align-items: flex-end; }
    .pb-8 { padding-bottom: 8px; }
    
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
    .gap-4 { gap: 4px; }
    .py-8 { padding-top: 8px; padding-bottom: 8px; }
    .py-24 { padding-top: 24px; padding-bottom: 24px; }
    .text-center { text-align: center; }
    .mb-0 { margin-bottom: 0; }
  `]
})
export class Categories implements OnInit {
  private readonly categoryService = inject(ProductCategoryService);
  private readonly toastService = inject(ToastService);

  protected treeData = signal<any[]>([]);
  protected flatCategoriesList = signal<any[]>([]);
  protected expandedNodes = signal<Set<string>>(new Set());
  protected loading = signal(false);
  protected submitting = signal(false);
  protected showModal = signal(false);
  protected isEditMode = signal(false);

  protected searchQuery = '';
  protected currentCategory = {
    id: '',
    parentId: null as string | null,
    parentName: '',
    code: '',
    name: '',
    description: '',
    isActive: true,
    displayOrder: 0
  };

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.loading.set(true);
    this.categoryService.getTree().subscribe({
      next: (data) => {
        this.treeData.set(data);
        this.flattenCategories(data);
        
        // Auto-expand all root nodes on load
        const roots = data.map(n => n.id);
        this.expandedNodes.update(set => {
          roots.forEach(r => set.add(r));
          return new Set(set);
        });

        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load categories.');
        this.loading.set(false);
      }
    });
  }

  flattenCategories(nodes: any[]) {
    const result: any[] = [];
    function recurse(list: any[]) {
      for (const node of list) {
        result.push({ id: node.id, code: node.code, name: node.name, parentId: node.parentId });
        if (node.children && node.children.length > 0) {
          recurse(node.children);
        }
      }
    }
    recurse(nodes);
    this.flatCategoriesList.set(result);
  }

  toggleExpand(id: string) {
    this.expandedNodes.update(set => {
      if (set.has(id)) {
        set.delete(id);
      } else {
        set.add(id);
      }
      return new Set(set);
    });
  }

  shouldShowNode(node: any): boolean {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) return true;

    // Check if current node or any descendant matches search query
    function matches(n: any): boolean {
      if (n.code.toLowerCase().includes(query) || n.name.toLowerCase().includes(query) || (n.description && n.description.toLowerCase().includes(query))) {
        return true;
      }
      if (n.children && n.children.length > 0) {
        return n.children.some((c: any) => matches(c));
      }
      return false;
    }
    return matches(node);
  }

  onSearchChange() {
    // If searching, expand everything
    if (this.searchQuery.trim()) {
      this.expandedNodes.update(set => {
        this.flatCategoriesList().forEach(c => set.add(c.id));
        return new Set(set);
      });
    }
  }

  openCreateModal() {
    this.isEditMode.set(false);
    this.currentCategory = {
      id: '',
      parentId: null,
      parentName: '',
      code: '',
      name: '',
      description: '',
      isActive: true,
      displayOrder: 0
    };
    this.showModal.set(true);
  }

  openCreateChildModal(parentNode: any) {
    this.isEditMode.set(false);
    this.currentCategory = {
      id: '',
      parentId: parentNode.id,
      parentName: parentNode.name,
      code: '',
      name: '',
      description: '',
      isActive: true,
      displayOrder: parentNode.children.length
    };
    this.showModal.set(true);
  }

  openEditModal(node: any) {
    this.isEditMode.set(true);
    
    // Find parent name
    let parentName = '';
    if (node.parentId) {
      const parent = this.flatCategoriesList().find(c => c.id === node.parentId);
      if (parent) parentName = parent.name;
    }

    this.currentCategory = {
      id: node.id,
      parentId: node.parentId,
      parentName: parentName,
      code: node.code,
      name: node.name,
      description: node.description || '',
      isActive: node.isActive,
      displayOrder: node.displayOrder
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  getValidParents(): any[] {
    // Exclude current category and all of its descendants to prevent circular parenting
    const currentId = this.currentCategory.id;
    if (!currentId) return this.flatCategoriesList();

    const childrenIds = new Set<string>();
    const tree = this.treeData();

    function findAndGatherChildren(nodes: any[], targetId: string): boolean {
      for (const n of nodes) {
        if (n.id === targetId) {
          gatherDescendants(n);
          return true;
        }
        if (n.children && n.children.length > 0) {
          if (findAndGatherChildren(n.children, targetId)) return true;
        }
      }
      return false;
    }

    function gatherDescendants(node: any) {
      childrenIds.add(node.id);
      if (node.children && node.children.length > 0) {
        for (const child of node.children) {
          gatherDescendants(child);
        }
      }
    }

    findAndGatherChildren(tree, currentId);

    return this.flatCategoriesList().filter(c => !childrenIds.has(c.id));
  }

  onSubmit(form: any) {
    if (form.invalid) return;
    this.submitting.set(true);

    if (this.isEditMode()) {
      this.categoryService.update(this.currentCategory.id, this.currentCategory).subscribe({
        next: () => {
          this.toastService.success('Category updated successfully.');
          this.closeModal();
          this.loadCategories();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to update category.');
          this.submitting.set(false);
        }
      });
    } else {
      this.categoryService.create(this.currentCategory).subscribe({
        next: () => {
          this.toastService.success('Category created successfully.');
          
          // Auto expand the parent
          if (this.currentCategory.parentId) {
            this.expandedNodes.update(set => {
              set.add(this.currentCategory.parentId!);
              return new Set(set);
            });
          }

          this.closeModal();
          this.loadCategories();
          this.submitting.set(false);
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to create category.');
          this.submitting.set(false);
        }
      });
    }
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this category?')) {
      this.loading.set(true);
      this.categoryService.delete(id).subscribe({
        next: () => {
          this.toastService.success('Category deleted successfully.');
          this.loadCategories();
        },
        error: (err) => {
          this.toastService.error(err?.error?.message || 'Failed to delete category.');
          this.loading.set(false);
        }
      });
    }
  }
}

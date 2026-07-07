import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProjectService } from '../../core/services/project.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LoadingSpinner],
  template: `
    <div class="projects-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Upper Controls -->
      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="search-box">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search projects..." 
            [(ngModel)]="searchQuery"
            (input)="onSearchChange()"
          />
        </div>
        
        @if (canCreateProject()) {
          <button class="btn btn-primary" (click)="openCreateModal()">
            <span class="material-symbols-rounded">add</span>
            New Project
          </button>
        }
      </div>

      <!-- Projects Grid -->
      @if (filteredProjects().length === 0) {
        <div class="glass-card text-center py-48 text-muted">
          <span class="material-symbols-rounded empty-icon">folder_off</span>
          <h3>No projects found</h3>
          <p>Create a new project or get invited to one to start managing tasks.</p>
        </div>
      } @else {
        <div class="grid grid-cols-3 gap-24">
          @for (project of filteredProjects(); track project.id) {
            <div class="glass-card interactive project-card animate-fade-in-up" [routerLink]="['/projects', project.id]">
              <div class="card-top flex justify-between align-center mb-12">
                <span class="badge" [ngClass]="'badge-status-' + project.status.toLowerCase()">
                  {{ project.status }}
                </span>
                <span class="owner-badge" [title]="'Owned by ' + project.ownerFullName">
                  <span class="material-symbols-rounded">person</span>
                  {{ project.ownerFullName }}
                </span>
              </div>
              <h3 class="project-title mb-8">{{ project.name }}</h3>
              <p class="project-desc">{{ project.description || 'No description provided.' }}</p>
              <div class="card-footer mt-16 pt-16 flex align-center justify-between">
                <span class="date-text">
                  Created {{ project.createdAt | date:'shortDate' }}
                </span>
                <span class="view-link flex align-center gap-8">
                  Open Project
                  <span class="material-symbols-rounded">arrow_right_alt</span>
                </span>
              </div>
            </div>
          }
        </div>
      }

      <!-- Pagination -->
      @if (totalProjects() > pageSize) {
        <div class="flex justify-center align-center gap-16 mt-24">
          <button class="btn btn-outline" [disabled]="currentPage() === 1" (click)="goToPage(currentPage() - 1)">
            Previous
          </button>
          <span class="page-indicator">Page {{ currentPage() }} of {{ totalPages() }}</span>
          <button class="btn btn-outline" [disabled]="currentPage() === totalPages()" (click)="goToPage(currentPage() + 1)">
            Next
          </button>
        </div>
      }

      <!-- Create Project Modal -->
      @if (showCreateModal()) {
        <div class="modal-overlay" (click)="closeCreateModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Create New Project</h3>
              <button class="close-btn" (click)="closeCreateModal()">&times;</button>
            </div>
            
            <form #createForm="ngForm" (ngSubmit)="onCreateSubmit(createForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="proj-name">Project Name</label>
                  <input 
                    type="text" 
                    id="proj-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="newProject.name" 
                    #projName="ngModel" 
                    required 
                    maxlength="200"
                    placeholder="e.g. Website Redesign"
                  />
                  @if (projName.invalid && (projName.dirty || projName.touched)) {
                    <span class="error-text">Project name is required (max 200 chars).</span>
                  }
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="proj-desc">Description</label>
                  <textarea 
                    id="proj-desc" 
                    name="description" 
                    class="form-input form-textarea" 
                    [(ngModel)]="newProject.description" 
                    maxlength="2000"
                    placeholder="Briefly describe the project goals..."
                  ></textarea>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeCreateModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="createForm.invalid || creating()">
                  {{ creating() ? 'Creating...' : 'Create Project' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .projects-container {
      width: 100%;
    }
    .search-input {
      min-width: 280px;
    }
    .project-card {
      display: flex;
      flex-direction: column;
      height: 200px;
    }
    .project-title {
      font-size: 1.15rem;
      font-weight: 600;
      color: var(--text-main);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .project-desc {
      color: var(--text-muted);
      font-size: 0.85rem;
      line-height: 1.5;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
      text-overflow: ellipsis;
      flex-grow: 1;
    }
    .owner-badge {
      font-size: 0.75rem;
      color: var(--text-muted);
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-weight: 500;
      background-color: #f1f5f9;
      padding: 2px 8px;
      border-radius: 6px;
    }
    .owner-badge span {
      font-size: 14px;
    }
    .card-footer {
      border-top: 1px solid var(--border);
    }
    .date-text {
      font-size: 0.75rem;
      color: var(--text-light);
    }
    .view-link {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--primary);
    }
    .view-link span {
      font-size: 16px;
      transition: transform var(--transition-fast);
    }
    .project-card:hover .view-link span {
      transform: translateX(3px);
    }
    .py-48 {
      padding-top: 48px;
      padding-bottom: 48px;
    }
    .empty-icon {
      font-size: 48px;
      color: var(--text-light);
      margin-bottom: 12px;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: var(--text-light);
      line-height: 1;
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
    .page-indicator {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-muted);
    }
  `]
})
export class Projects implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  protected projects = signal<any[]>([]);
  protected filteredProjects = signal<any[]>([]);
  protected loading = signal(false);

  // Pagination & Filtering state
  protected currentPage = signal(1);
  protected pageSize = 6;
  protected totalProjects = signal(0);
  protected totalPages = signal(0);
  protected searchQuery = '';

  // Modal State
  protected showCreateModal = signal(false);
  protected creating = signal(false);
  protected newProject = {
    name: '',
    description: ''
  };

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects() {
    this.loading.set(true);
    this.projectService.getProjects(this.currentPage(), this.pageSize).subscribe({
      next: (res) => {
        this.projects.set(res.items || []);
        this.totalProjects.set(res.totalCount || 0);
        this.totalPages.set(Math.ceil(this.totalProjects() / this.pageSize));
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load projects list.');
      }
    });
  }

  applyFilters() {
    if (this.searchQuery.trim()) {
      const lower = this.searchQuery.toLowerCase();
      this.filteredProjects.set(
        this.projects().filter(p => 
          p.name.toLowerCase().indexOf(lower) !== -1 || 
          (p.description && p.description.toLowerCase().indexOf(lower) !== -1)
        )
      );
    } else {
      this.filteredProjects.set(this.projects());
    }
  }

  onSearchChange() {
    this.applyFilters();
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadProjects();
  }

  canCreateProject(): boolean {
    const user = this.authService.currentUser();
    return user?.roles?.includes('Admin') || user?.roles?.includes('ProjectManager');
  }

  openCreateModal() {
    this.newProject = { name: '', description: '' };
    this.showCreateModal.set(true);
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
  }

  onCreateSubmit(form: any) {
    if (form.invalid) return;

    this.creating.set(true);
    this.projectService.createProject(this.newProject).subscribe({
      next: () => {
        this.creating.set(false);
        this.showCreateModal.set(false);
        this.toastService.success('Project created successfully.');
        this.loadProjects();
      },
      error: (err) => {
        this.creating.set(false);
        const msg = err.error?.message || 'Failed to create project.';
        this.toastService.error(msg);
      }
    });
  }
}

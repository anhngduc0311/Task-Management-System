import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { ProjectService } from '../../core/services/project.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { TaskDetailModal } from '../tasks/task-detail-modal';

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner, TaskDetailModal],
  template: `
    <div class="my-tasks-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <div class="filters-left flex align-center gap-16 flex-wrap w-full">
          <input 
            type="text" 
            class="form-input search-input" 
            placeholder="Search tasks..." 
            [(ngModel)]="searchQuery"
            (input)="onFilterChange()"
          />
          <select class="form-select status-select" [(ngModel)]="statusFilter" (change)="onFilterChange()">
            <option value="">All Statuses</option>
            <option value="Todo">Todo</option>
            <option value="InProgress">In Progress</option>
            <option value="InReview">In Review</option>
            <option value="Done">Done</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          <select class="form-select priority-select" [(ngModel)]="priorityFilter" (change)="onFilterChange()">
            <option value="">All Priorities</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </div>
      </div>

      <div class="glass-card">
        <div class="responsive-table-container">
          <table class="responsive-table">
            <thead>
              <tr>
                <th class="hide-mobile">Project</th>
                <th>Task Title</th>
                <th class="hide-mobile">Priority</th>
                <th>Status</th>
                <th>Due Date</th>
              </tr>
            </thead>
            <tbody>
              @if (filteredTasks().length === 0) {
                <tr>
                  <td colspan="5" class="text-center py-24 text-muted">
                    No tasks found matching your filters.
                  </td>
                </tr>
              } @else {
                @for (task of filteredTasks(); track task.id) {
                  <tr (click)="openTaskDetails(task.id)" class="clickable-row animate-fade-in-up">
                    <td class="project-col hide-mobile">
                      <span class="material-symbols-rounded">folder_open</span>
                      {{ task.projectName }}
                    </td>
                    <td>
                      <div class="task-title-cell">{{ task.title }}</div>
                      <!-- Inline mobile metadata -->
                      <div class="mobile-only-meta show-mobile mt-4" style="display: none; flex-wrap: wrap; gap: 8px; align-items: center; font-size: 0.75rem; color: var(--text-muted);">
                        <span style="display: inline-flex; align-items: center; gap: 2px;">
                          <span class="material-symbols-rounded" style="font-size: 14px;">folder_open</span>
                          {{ task.projectName }}
                        </span>
                        <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()" style="padding: 2px 6px; font-size: 0.65rem;">
                          {{ task.priority }}
                        </span>
                      </div>
                      @if (task.description) {
                        <div class="task-desc-cell">{{ task.description }}</div>
                      }
                    </td>
                    <td class="hide-mobile">
                      <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                        {{ task.priority }}
                      </span>
                    </td>
                    <td>
                      <span class="badge" [ngClass]="'badge-status-' + task.status.toLowerCase()">
                        {{ getStatusLabel(task.status) }}
                      </span>
                    </td>
                    <td>
                      @if (task.dueDate) {
                        <span [ngClass]="{ 'overdue': isOverdue(task.dueDate) && task.status !== 'Done' }">
                          {{ task.dueDate | date:'mediumDate' }}
                        </span>
                      } @else {
                        <span class="text-light">-</span>
                      }
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Task Detail Modal Component -->
      @if (selectedTaskId()) {
        <app-task-detail-modal
          [taskId]="selectedTaskId()!"
          (close)="closeTaskDetails()"
          (taskUpdated)="onTaskUpdated()"
        ></app-task-detail-modal>
      }
    </div>
  `,
  styles: [`
    .my-tasks-container {
      width: 100%;
    }
    .search-input {
      max-width: 300px;
    }
    .status-select, .priority-select {
      max-width: 160px;
    }
    .clickable-row {
      cursor: pointer;
    }
    .project-col {
      font-weight: 500;
      color: var(--text-muted);
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .project-col span {
      font-size: 16px;
    }
    .task-title-cell {
      font-weight: 600;
      color: var(--text-main);
    }
    .task-desc-cell {
      font-size: 0.8rem;
      color: var(--text-light);
      margin-top: 4px;
      max-width: 320px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .overdue {
      color: var(--danger);
      font-weight: 600;
    }
    .text-center {
      text-align: center;
    }
    .py-24 {
      padding-top: 24px;
      padding-bottom: 24px;
    }
    @media (max-width: 768px) {
      .filters-left {
        flex-direction: column;
        align-items: stretch !important;
        gap: 12px !important;
      }
      .search-input, .status-select, .priority-select {
        max-width: 100% !important;
        width: 100% !important;
      }
    }
  `]
})
export class MyTasks implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly projectService = inject(ProjectService);
  private readonly toastService = inject(ToastService);

  protected tasks = signal<any[]>([]);
  protected filteredTasks = signal<any[]>([]);
  protected loading = signal(false);

  // Filters state
  protected searchQuery: string = '';
  protected statusFilter: string = '';
  protected priorityFilter: string = '';

  // Task Drawer state
  protected selectedTaskId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks() {
    this.loading.set(true);

    this.projectService.getProjects(1, 100).subscribe({
      next: (res) => {
        const projects = res.items || [];
        
        this.taskService.getMyTasks().subscribe({
          next: (tasksList) => {
            const mapped = tasksList.map((t: any) => {
              const proj = projects.find((p: any) => p.id === t.projectId);
              return {
                ...t,
                projectName: proj ? proj.name : 'Project'
              };
            });
            this.tasks.set(mapped);
            this.applyFilters();
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
            this.toastService.error('Failed to load my tasks.');
          }
        });
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load projects list.');
      }
    });
  }

  onFilterChange() {
    this.applyFilters();
  }

  applyFilters() {
    let result = [...this.tasks()];

    if (this.searchQuery.trim()) {
      const lower = this.searchQuery.toLowerCase();
      result = result.filter(t => t.title.toLowerCase().indexOf(lower) !== -1 || (t.description && t.description.toLowerCase().indexOf(lower) !== -1));
    }

    if (this.statusFilter) {
      result = result.filter(t => t.status === this.statusFilter);
    }

    if (this.priorityFilter) {
      result = result.filter(t => t.priority === this.priorityFilter);
    }

    this.filteredTasks.set(result);
  }

  getStatusLabel(status: string): string {
    if (status === 'InProgress') return 'In Progress';
    if (status === 'InReview') return 'In Review';
    return status;
  }

  isOverdue(dueDateStr: string): boolean {
    const dueDate = new Date(dueDateStr);
    dueDate.setHours(23, 59, 59, 999);
    return dueDate.getTime() < Date.now();
  }

  openTaskDetails(taskId: string) {
    this.selectedTaskId.set(taskId);
  }

  closeTaskDetails() {
    this.selectedTaskId.set(null);
  }

  onTaskUpdated() {
    this.loadTasks();
  }
}

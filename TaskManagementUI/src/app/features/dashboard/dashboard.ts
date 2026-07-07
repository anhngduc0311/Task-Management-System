import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProjectService } from '../../core/services/project.service';
import { TaskService } from '../../core/services/task.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingSpinner],
  template: `
    <div class="dashboard-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Stats Grid -->
      <div class="grid grid-cols-3 gap-24 mb-24">
        <!-- Stat Card 1 -->
        <div class="glass-card stat-card">
          <div class="stat-icon icon-projects">
            <span class="material-symbols-rounded">folder</span>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ activeProjectsCount() }}</div>
            <div class="stat-label">Active Projects</div>
          </div>
        </div>

        <!-- Stat Card 2 -->
        <div class="glass-card stat-card">
          <div class="stat-icon icon-tasks">
            <span class="material-symbols-rounded">assignment_late</span>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ myPendingTasksCount() }}</div>
            <div class="stat-label">My Pending Tasks</div>
          </div>
        </div>

        <!-- Stat Card 3 -->
        <div class="glass-card stat-card">
          <div class="stat-icon icon-completed">
            <span class="material-symbols-rounded">task_alt</span>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ myCompletedTasksCount() }}</div>
            <div class="stat-label">My Completed Tasks</div>
          </div>
        </div>
      </div>

      <!-- Main Layout -->
      <div class="dashboard-content grid grid-cols-2 gap-24">
        <!-- My Tasks Summary -->
        <div class="glass-card list-section">
          <div class="section-header flex justify-between align-center mb-16">
            <h3>Urgent Tasks</h3>
            <a routerLink="/my-tasks" class="view-all-link">View All</a>
          </div>

          <div class="task-list">
            @if (myTasks().length === 0) {
              <div class="empty-state">
                <span class="material-symbols-rounded empty-icon">sentiment_satisfied</span>
                <p>No tasks assigned to you. Enjoy your day!</p>
              </div>
            } @else {
              @for (task of getUrgentTasks(); track task.id) {
                <div class="task-row animate-fade-in-up" [routerLink]="['/projects', task.projectId]">
                  <div class="task-info">
                    <div class="task-title">{{ task.title }}</div>
                    <div class="task-project">
                      <span class="material-symbols-rounded">folder_open</span>
                      {{ task.projectName || 'Project' }}
                    </div>
                  </div>
                  <div class="task-meta">
                    <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                      {{ task.priority }}
                    </span>
                    @if (task.dueDate) {
                      <div class="task-due" [ngClass]="{ 'overdue': isOverdue(task.dueDate) }">
                        <span class="material-symbols-rounded">calendar_month</span>
                        {{ task.dueDate | date:'mediumDate' }}
                      </div>
                    }
                  </div>
                </div>
              }
            }
          </div>
        </div>

        <!-- Active Projects Summary -->
        <div class="glass-card list-section">
          <div class="section-header flex justify-between align-center mb-16">
            <h3>My Projects</h3>
            <a routerLink="/projects" class="view-all-link">View All</a>
          </div>

          <div class="project-list">
            @if (projects().length === 0) {
              <div class="empty-state">
                <span class="material-symbols-rounded empty-icon">work_off</span>
                <p>You are not in any active projects.</p>
              </div>
            } @else {
              @for (project of projects().slice(0, 5); track project.id) {
                <div class="project-row animate-fade-in-up" [routerLink]="['/projects', project.id]">
                  <div class="project-info">
                    <div class="project-title">{{ project.name }}</div>
                    <p class="project-desc">{{ project.description || 'No description provided.' }}</p>
                  </div>
                  <div class="project-meta">
                    <span class="badge badge-status-todo">
                      {{ project.status }}
                    </span>
                  </div>
                </div>
              }
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      width: 100%;
    }

    /* Stat Card */
    .stat-card {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 20px 24px;
    }
    .stat-icon {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .stat-icon span {
      font-size: 24px;
    }
    .icon-projects {
      background-color: var(--primary-light);
      color: var(--primary);
    }
    .icon-tasks {
      background-color: var(--warning-bg);
      color: var(--warning);
    }
    .icon-completed {
      background-color: var(--success-bg);
      color: var(--success);
    }
    .stat-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--text-main);
    }
    .stat-label {
      font-size: 0.85rem;
      color: var(--text-muted);
      font-weight: 500;
    }

    /* Summary list views */
    .list-section {
      height: 400px;
      display: flex;
      flex-direction: column;
    }
    .view-all-link {
      font-size: 0.85rem;
      font-weight: 600;
    }
    .task-list, .project-list {
      flex-grow: 1;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding-right: 4px;
    }
    .task-row, .project-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 12px 16px;
      background-color: #ffffff;
      border: 1px solid var(--border);
      border-radius: 10px;
      cursor: pointer;
      transition: all var(--transition-fast);
    }
    .task-row:hover, .project-row:hover {
      border-color: var(--primary);
      transform: translateY(-1px);
      box-shadow: var(--shadow-sm);
    }
    .task-info, .project-info {
      min-width: 0;
    }
    .task-title, .project-title {
      font-size: 0.925rem;
      font-weight: 600;
      color: var(--text-main);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .task-project {
      font-size: 0.775rem;
      color: var(--text-muted);
      display: flex;
      align-items: center;
      gap: 4px;
      margin-top: 4px;
    }
    .task-project span {
      font-size: 14px;
    }
    .task-meta, .project-meta {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 6px;
      flex-shrink: 0;
    }
    .task-due {
      font-size: 0.75rem;
      color: var(--text-light);
      display: flex;
      align-items: center;
      gap: 4px;
    }
    .task-due.overdue {
      color: var(--danger);
      font-weight: 600;
    }
    .task-due span {
      font-size: 14px;
    }
    .project-desc {
      font-size: 0.8rem;
      color: var(--text-muted);
      margin-top: 4px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      text-align: center;
      color: var(--text-light);
      gap: 8px;
    }
    .empty-icon {
      font-size: 40px;
    }
    .empty-state p {
      font-size: 0.875rem;
    }
  `]
})
export class Dashboard implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);
  private readonly toastService = inject(ToastService);

  protected projects = signal<any[]>([]);
  protected myTasks = signal<any[]>([]);
  protected loading = signal(false);

  protected activeProjectsCount = signal(0);
  protected myPendingTasksCount = signal(0);
  protected myCompletedTasksCount = signal(0);

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.loading.set(true);

    this.projectService.getProjects(1, 100).subscribe({
      next: (res) => {
        this.projects.set(res.items || []);
        this.activeProjectsCount.set(this.projects().filter(p => p.status === 'Active').length);
        
        this.taskService.getMyTasks().subscribe({
          next: (tasks) => {
            const mappedTasks = tasks.map((t: any) => {
              const proj = this.projects().find(p => p.id === t.projectId);
              return {
                ...t,
                projectName: proj ? proj.name : 'Project'
              };
            });
            this.myTasks.set(mappedTasks);
            this.myPendingTasksCount.set(this.myTasks().filter(t => t.status !== 'Done' && t.status !== 'Cancelled').length);
            this.myCompletedTasksCount.set(this.myTasks().filter(t => t.status === 'Done').length);
            this.loading.set(false);
          },
          error: (err) => {
            this.loading.set(false);
            this.toastService.error('Failed to load my tasks.');
          }
        });
      },
      error: (err) => {
        this.loading.set(false);
        this.toastService.error('Failed to load projects list.');
      }
    });
  }

  getUrgentTasks(): any[] {
    return this.myTasks()
      .filter(t => t.status !== 'Done' && t.status !== 'Cancelled')
      .sort((a, b) => {
        const priorities = { 'Critical': 4, 'High': 3, 'Medium': 2, 'Low': 1 };
        const pA = priorities[a.priority as keyof typeof priorities] || 0;
        const pB = priorities[b.priority as keyof typeof priorities] || 0;
        if (pA !== pB) return pB - pA;
        
        if (!a.dueDate) return 1;
        if (!b.dueDate) return -1;
        return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
      })
      .slice(0, 5);
  }

  isOverdue(dueDateStr: string): boolean {
    const dueDate = new Date(dueDateStr);
    dueDate.setHours(23, 59, 59, 999);
    return dueDate.getTime() < Date.now();
  }
}

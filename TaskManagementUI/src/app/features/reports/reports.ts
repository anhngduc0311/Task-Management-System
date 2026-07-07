import { Component, OnInit, inject, signal, computed, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

// Angular Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

// Core Services
import { ReportService } from '../../core/services/report.service';
import { ProjectService } from '../../core/services/project.service';
import { UserService } from '../../core/services/user.service';
import { ToastService } from '../../core/services/toast.service';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule
  ],
  template: `
    <div class="reports-view">
      <!-- Loading Overlay -->
      @if (loading()) {
        <div class="loading-overlay">
          <mat-spinner diameter="60" color="primary"></mat-spinner>
          <p>Processing report data...</p>
        </div>
      }

      <!-- Filter Controls Panel -->
      <mat-card class="glass-card filters-card mb-24">
        <mat-card-content>
          <div class="flex justify-between align-center mb-16 flex-wrap gap-12" style="border-bottom: 1px solid var(--border); padding-bottom: 12px; margin-bottom: 16px;">
            <div>
              <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--text-main); display: flex; align-items: center; gap: 8px; margin: 0;">
                <span class="material-symbols-rounded text-primary" style="font-size: 24px;">analytics</span>
                Work Performance & Reports
              </h3>
              <p style="font-size: 0.8rem; color: var(--text-muted); margin: 2px 0 0 0;">Filter and analyze task distribution, deadlines, and completion metrics.</p>
            </div>
            <button class="btn btn-outline btn-sm" (click)="resetFilters()" style="display: inline-flex; align-items: center; gap: 6px; padding: 6px 12px; border-radius: 8px; font-weight: 500; height: 36px; cursor: pointer;">
              <span class="material-symbols-rounded" style="font-size: 16px;">restart_alt</span>
              Reset Filters
            </button>
          </div>

          <div class="filter-grid">
            <!-- Project Filter -->
            <mat-form-field appearance="outline" class="filter-item">
              <mat-label>Project</mat-label>
              <span matPrefix class="material-symbols-rounded mr-8" style="color: var(--text-muted); font-size: 20px; vertical-align: middle;">folder_open</span>
              <mat-select [(ngModel)]="filters.projectId" (selectionChange)="onProjectChange()">
                <mat-option [value]="null">All Projects</mat-option>
                @for (p of projects(); track p.id) {
                  <mat-option [value]="p.id">{{ p.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <!-- Assignee Filter -->
            <mat-form-field appearance="outline" class="filter-item">
              <mat-label>Assignee</mat-label>
              <span matPrefix class="material-symbols-rounded mr-8" style="color: var(--text-muted); font-size: 20px; vertical-align: middle;">person</span>
              <mat-select [(ngModel)]="filters.assigneeId" (selectionChange)="refreshReports()">
                <mat-option [value]="null">All Assignees</mat-option>
                @for (u of assignees(); track u.id) {
                  <mat-option [value]="u.id">{{ u.fullName }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <!-- Status Filter -->
            <mat-form-field appearance="outline" class="filter-item">
              <mat-label>Status</mat-label>
              <span matPrefix class="material-symbols-rounded mr-8" style="color: var(--text-muted); font-size: 20px; vertical-align: middle;">flaky</span>
              <mat-select [(ngModel)]="filters.status" (selectionChange)="refreshReports()">
                <mat-option [value]="null">All Statuses</mat-option>
                <mat-option value="Todo">Todo</mat-option>
                <mat-option value="InProgress">In Progress</mat-option>
                <mat-option value="InReview">In Review</mat-option>
                <mat-option value="Done">Done</mat-option>
                <mat-option value="Cancelled">Cancelled</mat-option>
              </mat-select>
            </mat-form-field>

            <!-- Priority Filter -->
            <mat-form-field appearance="outline" class="filter-item">
              <mat-label>Priority</mat-label>
              <span matPrefix class="material-symbols-rounded mr-8" style="color: var(--text-muted); font-size: 20px; vertical-align: middle;">label_important</span>
              <mat-select [(ngModel)]="filters.priority" (selectionChange)="refreshReports()">
                <mat-option [value]="null">All Priorities</mat-option>
                <mat-option value="Low">Low</mat-option>
                <mat-option value="Medium">Medium</mat-option>
                <mat-option value="High">High</mat-option>
                <mat-option value="Critical">Critical</mat-option>
              </mat-select>
            </mat-form-field>

            <!-- Date Range Filter -->
            <mat-form-field appearance="outline" class="filter-item date-range-field">
              <mat-label>Date Range (Created At)</mat-label>
              <span matPrefix class="material-symbols-rounded mr-8" style="color: var(--text-muted); font-size: 20px; vertical-align: middle;">date_range</span>
              <mat-date-range-input [rangePicker]="picker">
                <input matStartDate [(ngModel)]="filters.dateFrom" placeholder="Start date" (dateChange)="refreshReports()">
                <input matEndDate [(ngModel)]="filters.dateTo" placeholder="End date" (dateChange)="refreshReports()">
              </mat-date-range-input>
              <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-date-range-picker #picker></mat-date-range-picker>
            </mat-form-field>
          </div>

          <!-- Dynamic Fields Filters (Only if project selected) -->
          @if (filters.projectId && dynamicFieldsList().length > 0) {
            <div class="dynamic-filters-section mt-16 pt-16" style="border-top: 1px dashed var(--border);">
              <h4 class="mb-12" style="font-size: 0.9rem; color: var(--text-muted); display: flex; align-items: center; gap: 8px;">
                <span class="material-symbols-rounded" style="font-size: 18px; width: 18px; height: 18px;">tune</span>
                Dynamic Fields Filters
              </h4>
              <div class="filter-grid">
                @for (df of dynamicFieldsList(); track df.id) {
                  <mat-form-field appearance="outline" class="filter-item">
                    <mat-label>{{ df.fieldName }}</mat-label>
                    <input matInput [(ngModel)]="dynamicFieldFilters[df.fieldKey]" (input)="onDynamicFilterChange()" placeholder="Filter by {{ df.fieldName }}...">
                  </mat-form-field>
                }
              </div>
            </div>
          }
        </mat-card-content>
      </mat-card>

      <!-- Error State -->
      @if (error()) {
        <div class="error-banner mb-24 animate-fade-in">
          <span class="material-symbols-rounded">error</span>
          <div class="error-message">
            <h4>An error occurred while loading reports</h4>
            <p>{{ error() }}</p>
          </div>
          <button class="btn btn-outline btn-sm" (click)="loadInitialData()">Retry</button>
        </div>
      }

      <!-- KPI Summary Cards -->
      <div class="kpi-grid mb-24">
        <!-- Card 1: Total Tasks -->
        <mat-card class="glass-card kpi-card">
          <mat-card-content>
            <div class="flex align-center justify-between">
              <div>
                <div class="kpi-label">Total Tasks</div>
                <div class="kpi-value text-primary" style="font-size: 2.25rem; font-weight: 800; color: #4f46e5;">
                  {{ summary().totalTasks }}
                </div>
              </div>
              <div class="kpi-icon-bg bg-primary-light">
                <span class="material-symbols-rounded">assignment</span>
              </div>
            </div>
            <div class="kpi-footer mt-12" style="font-size: 0.75rem; color: var(--text-muted);">
              All tasks in scope
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Card 2: Completed Tasks -->
        <mat-card class="glass-card kpi-card">
          <mat-card-content>
            <div class="flex align-center justify-between">
              <div>
                <div class="kpi-label">Completed Tasks</div>
                <div class="kpi-value text-success" style="font-size: 2.25rem; font-weight: 800; color: #10b981;">
                  {{ summary().completedTasks }}
                </div>
              </div>
              <div class="kpi-icon-bg bg-success-light">
                <span class="material-symbols-rounded">task_alt</span>
              </div>
            </div>
            <div class="kpi-footer mt-12" style="font-size: 0.75rem; color: var(--text-muted); display: flex; align-items: center; gap: 4px;">
              <span class="material-symbols-rounded" style="font-size: 14px; color: var(--success); vertical-align: middle;">done_all</span>
              <span><strong>{{ summary().completedOnTime }}</strong> on-time</span>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Card 3: Overdue Tasks -->
        <mat-card class="glass-card kpi-card">
          <mat-card-content>
            <div class="flex align-center justify-between">
              <div>
                <div class="kpi-label">Overdue Tasks</div>
                <div class="kpi-value text-danger" style="font-size: 2.25rem; font-weight: 800; color: #ef4444;">
                  {{ summary().overdueTasks }}
                </div>
              </div>
              <div class="kpi-icon-bg bg-danger-light">
                <span class="material-symbols-rounded">event_busy</span>
              </div>
            </div>
            <div class="kpi-footer mt-12" style="font-size: 0.75rem; color: var(--text-muted); display: flex; align-items: center; gap: 4px;">
              <span class="material-symbols-rounded" style="font-size: 14px; color: {{ summary().overdueTasks > 0 ? '#ef4444' : 'var(--text-muted)' }}; vertical-align: middle;">info</span>
              <span>Needs attention</span>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Card 4: Completion Rate -->
        <mat-card class="glass-card kpi-card">
          <mat-card-content>
            <div class="flex align-center justify-between">
              <div>
                <div class="kpi-label">Completion Rate</div>
                <div class="kpi-value text-secondary" style="font-size: 2.25rem; font-weight: 800; color: #8b5cf6;">
                  {{ summary().completionRate }}%
                </div>
              </div>
              <div class="kpi-progress-circle" style="position: relative; width: 48px; height: 48px;">
                <svg width="48" height="48" viewBox="0 0 36 36" class="circular-chart" style="transform: rotate(-90deg);">
                  <path class="circle-bg" stroke="#f1f5f9" stroke-width="3" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                  <path class="circle" [attr.stroke-dasharray]="summary().completionRate + ', 100'" stroke="#8b5cf6" stroke-width="3" stroke-linecap="round" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" style="transition: stroke-dasharray 0.5s ease;" />
                </svg>
              </div>
            </div>
            <div class="kpi-footer mt-12" style="font-size: 0.75rem; color: var(--text-muted); display: flex; align-items: center; gap: 4px;">
              <span class="material-symbols-rounded" style="font-size: 14px; color: #8b5cf6; vertical-align: middle;">trending_up</span>
              <span>Based on total tasks ratio</span>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Charts Section -->
      <div class="charts-grid mb-24">
        <!-- Chart 1: Tasks by Status (Donut Chart) -->
        <mat-card class="glass-card chart-card">
          <mat-card-header>
            <mat-card-title>Tasks by Status</mat-card-title>
          </mat-card-header>
          <mat-card-content class="flex align-center justify-center flex-col pt-16">
            @if (summary().totalTasks === 0) {
              <div class="no-data-msg">No tasks matching filters.</div>
            } @else {
              <div class="flex align-center justify-around w-full flex-wrap gap-16">
                <!-- SVG Donut -->
                <div class="svg-container" style="position: relative; width: 150px; height: 150px;">
                  <svg width="150" height="150" viewBox="0 0 120 120" style="transform: rotate(-90deg);">
                    <circle cx="60" cy="60" r="50" fill="transparent" stroke="#f1f5f9" stroke-width="12"></circle>
                    @for (seg of statusSegments(); track seg.key) {
                      <circle cx="60" cy="60" r="50" fill="transparent"
                              [attr.stroke]="seg.color"
                              stroke-width="12"
                              [attr.stroke-dasharray]="seg.strokeLength + ' ' + (314.159 - seg.strokeLength)"
                              [attr.stroke-dashoffset]="seg.strokeOffset"
                              style="transition: all 0.5s ease; cursor: pointer;">
                      </circle>
                    }
                  </svg>
                  <div class="donut-center-text">
                    <strong>{{ summary().totalTasks }}</strong>
                    <span>Tasks</span>
                  </div>
                </div>

                <!-- Legend -->
                <div class="legend-list">
                  @for (seg of statusSegments(); track seg.key) {
                    <div class="legend-item">
                      <span class="legend-dot" [style.background-color]="seg.color"></span>
                      <span class="legend-label">{{ seg.key }}</span>
                      <span class="legend-count">{{ seg.count }} ({{ seg.percent | number:'1.0-1' }}%)</span>
                    </div>
                  }
                </div>
              </div>
            }
          </mat-card-content>
        </mat-card>

        <!-- Chart 2: Tasks by Priority (Bar Column Chart) -->
        <mat-card class="glass-card chart-card">
          <mat-card-header>
            <mat-card-title>Tasks by Priority</mat-card-title>
          </mat-card-header>
          <mat-card-content class="flex flex-col justify-between pt-16" style="min-height: 200px;">
            @if (summary().totalTasks === 0) {
              <div class="no-data-msg m-auto">No tasks matching filters.</div>
            } @else {
              <div class="priority-chart-container">
                @for (bar of priorityBars(); track bar.key) {
                  <div class="priority-bar-wrapper">
                    <span class="bar-count">{{ bar.count }}</span>
                    <div class="priority-bar"
                         [style.height.px]="bar.height"
                         [style.background]="bar.color"
                         [title]="bar.key + ': ' + bar.count + ' tasks'">
                    </div>
                    <span class="bar-label">{{ bar.key }}</span>
                  </div>
                }
              </div>
            }
          </mat-card-content>
        </mat-card>

        <!-- Chart 3: Tasks by Assignee (Horizontal Bar Ranking) -->
        <mat-card class="glass-card chart-card">
          <mat-card-header>
            <mat-card-title>Top Assignees</mat-card-title>
          </mat-card-header>
          <mat-card-content class="pt-16">
            @if (assigneeStats().length === 0) {
              <div class="no-data-msg text-center py-24">No tasks assigned yet.</div>
            } @else {
              <div class="assignee-chart-list">
                @for (astat of assigneeStats().slice(0, 4); track astat.assigneeId) {
                  <div class="assignee-stat-row">
                    <div class="flex justify-between align-center mb-4" style="font-size: 0.85rem;">
                      <span style="font-weight: 500; color: var(--text-main);">{{ astat.assigneeName }}</span>
                      <span style="font-weight: 600; color: var(--text-muted);">{{ astat.taskCount }} tasks</span>
                    </div>
                    <div class="progress-bar-bg">
                      <div class="progress-done"
                           [style.width.%]="(astat.completedCount / astat.taskCount) * 100"
                           [title]="astat.completedCount + ' tasks completed'"></div>
                      <div class="progress-pending"
                           [style.width.%]="((astat.taskCount - astat.completedCount - astat.overdueCount) / astat.taskCount) * 100"
                           [title]="(astat.taskCount - astat.completedCount - astat.overdueCount) + ' tasks in progress'"></div>
                      <div class="progress-overdue"
                           [style.width.%]="(astat.overdueCount / astat.taskCount) * 100"
                           [title]="astat.overdueCount + ' tasks overdue'"></div>
                    </div>
                    <div class="flex justify-between align-center mt-2" style="font-size: 0.7rem; color: var(--text-light);">
                      <span>{{ astat.completedCount }} Done</span>
                      <span>{{ astat.overdueCount }} Overdue</span>
                    </div>
                  </div>
                }
              </div>
            }
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Paginated Lists Section -->
      <mat-card class="glass-card table-card">
        <mat-card-content class="p-0">
          <mat-tab-group (selectedTabChange)="onTabChange($event.index)" color="primary">
            <!-- Overdue Tasks Tab -->
            <mat-tab>
              <ng-template matTabLabel>
                <span class="material-symbols-rounded" style="margin-right: 8px;">event_busy</span>
                Overdue Tasks ({{ overdueDataSource.data.length }})
              </ng-template>
              <div class="table-container pt-16">
                @if (overdueDataSource.data.length === 0) {
                  <div class="empty-state">
                    <span class="material-symbols-rounded empty-icon text-success">check_circle</span>
                    <h3>No overdue tasks!</h3>
                    <p>Awesome! All tasks are currently on track or completed.</p>
                  </div>
                } @else {
                  <table mat-table [dataSource]="overdueDataSource" class="reports-table">
                    <!-- Title Column -->
                    <ng-container matColumnDef="title">
                      <th mat-header-cell *matHeaderCellDef>Task</th>
                      <td mat-cell *matCellDef="let task">
                        <a [routerLink]="['/projects', task.projectId]" [queryParams]="{ task: task.id }" class="task-title-link">
                          {{ task.title }}
                        </a>
                      </td>
                    </ng-container>

                    <!-- Project Column -->
                    <ng-container matColumnDef="project">
                      <th mat-header-cell *matHeaderCellDef>Project</th>
                      <td mat-cell *matCellDef="let task">{{ task.projectName }}</td>
                    </ng-container>

                    <!-- Assignee Column -->
                    <ng-container matColumnDef="assignee">
                      <th mat-header-cell *matHeaderCellDef>Assignee</th>
                      <td mat-cell *matCellDef="let task">{{ task.assigneeName || 'Unassigned' }}</td>
                    </ng-container>

                    <!-- Status Column -->
                    <ng-container matColumnDef="status">
                      <th mat-header-cell *matHeaderCellDef>Status</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-status-' + task.status.toLowerCase()">
                          {{ task.status }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Priority Column -->
                    <ng-container matColumnDef="priority">
                      <th mat-header-cell *matHeaderCellDef>Priority</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                          {{ task.priority }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Due Date Column -->
                    <ng-container matColumnDef="dueDate">
                      <th mat-header-cell *matHeaderCellDef>Due Date</th>
                      <td mat-cell *matCellDef="let task" class="text-danger font-semibold">
                        {{ task.dueDate | date:'mediumDate' }}
                      </td>
                    </ng-container>

                    <tr mat-headerRowDef="displayedColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="interactive-row"></tr>
                  </table>
                  <mat-paginator #overduePaginator
                                 [length]="overdueTotalCount()"
                                 [pageSize]="pageSize()"
                                 [pageSizeOptions]="[5, 10, 20]"
                                 (page)="onPageChange($event)">
                  </mat-paginator>
                }
              </div>
            </mat-tab>

            <!-- Uncompleted Tasks Tab -->
            <mat-tab>
              <ng-template matTabLabel>
                <span class="material-symbols-rounded" style="margin-right: 8px;">rule</span>
                Uncompleted Tasks ({{ uncompletedDataSource.data.length }})
              </ng-template>
              <div class="table-container pt-16">
                @if (uncompletedDataSource.data.length === 0) {
                  <div class="empty-state">
                    <span class="material-symbols-rounded empty-icon text-muted">assignment_turned_in</span>
                    <h3>All tasks completed!</h3>
                    <p>There are no active or pending tasks matching the filters.</p>
                  </div>
                } @else {
                  <table mat-table [dataSource]="uncompletedDataSource" class="reports-table">
                    <!-- Title Column -->
                    <ng-container matColumnDef="title">
                      <th mat-header-cell *matHeaderCellDef>Task</th>
                      <td mat-cell *matCellDef="let task">
                        <a [routerLink]="['/projects', task.projectId]" [queryParams]="{ task: task.id }" class="task-title-link">
                          {{ task.title }}
                        </a>
                      </td>
                    </ng-container>

                    <!-- Project Column -->
                    <ng-container matColumnDef="project">
                      <th mat-header-cell *matHeaderCellDef>Project</th>
                      <td mat-cell *matCellDef="let task">{{ task.projectName }}</td>
                    </ng-container>

                    <!-- Assignee Column -->
                    <ng-container matColumnDef="assignee">
                      <th mat-header-cell *matHeaderCellDef>Assignee</th>
                      <td mat-cell *matCellDef="let task">{{ task.assigneeName || 'Unassigned' }}</td>
                    </ng-container>

                    <!-- Status Column -->
                    <ng-container matColumnDef="status">
                      <th mat-header-cell *matHeaderCellDef>Status</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-status-' + task.status.toLowerCase()">
                          {{ task.status }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Priority Column -->
                    <ng-container matColumnDef="priority">
                      <th mat-header-cell *matHeaderCellDef>Priority</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                          {{ task.priority }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Due Date Column -->
                    <ng-container matColumnDef="dueDate">
                      <th mat-header-cell *matHeaderCellDef>Due Date</th>
                      <td mat-cell *matCellDef="let task" [ngClass]="isOverdue(task.dueDate) ? 'text-danger font-semibold' : ''">
                        {{ task.dueDate ? (task.dueDate | date:'mediumDate') : 'No due date' }}
                      </td>
                    </ng-container>

                    <tr mat-headerRowDef="displayedColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="interactive-row"></tr>
                  </table>
                  <mat-paginator #uncompletedPaginator
                                 [length]="uncompletedTotalCount()"
                                 [pageSize]="pageSize()"
                                 [pageSizeOptions]="[5, 10, 20]"
                                 (page)="onPageChange($event)">
                  </mat-paginator>
                }
              </div>
            </mat-tab>

            <!-- Completed Tasks Tab -->
            <mat-tab>
              <ng-template matTabLabel>
                <span class="material-symbols-rounded" style="margin-right: 8px;">task_alt</span>
                Completed Tasks ({{ completedDataSource.data.length }})
              </ng-template>
              <div class="table-container pt-16">
                @if (completedDataSource.data.length === 0) {
                  <div class="empty-state">
                    <span class="material-symbols-rounded empty-icon text-muted">history</span>
                    <h3>No completed tasks</h3>
                    <p>No tasks were completed in this reporting period.</p>
                  </div>
                } @else {
                  <table mat-table [dataSource]="completedDataSource" class="reports-table">
                    <!-- Title Column -->
                    <ng-container matColumnDef="title">
                      <th mat-header-cell *matHeaderCellDef>Task</th>
                      <td mat-cell *matCellDef="let task">
                        <a [routerLink]="['/projects', task.projectId]" [queryParams]="{ task: task.id }" class="task-title-link">
                          {{ task.title }}
                        </a>
                      </td>
                    </ng-container>

                    <!-- Project Column -->
                    <ng-container matColumnDef="project">
                      <th mat-header-cell *matHeaderCellDef>Project</th>
                      <td mat-cell *matCellDef="let task">{{ task.projectName }}</td>
                    </ng-container>

                    <!-- Assignee Column -->
                    <ng-container matColumnDef="assignee">
                      <th mat-header-cell *matHeaderCellDef>Assignee</th>
                      <td mat-cell *matCellDef="let task">{{ task.assigneeName || 'Unassigned' }}</td>
                    </ng-container>

                    <!-- Status Column -->
                    <ng-container matColumnDef="status">
                      <th mat-header-cell *matHeaderCellDef>Status</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-status-' + task.status.toLowerCase()">
                          {{ task.status }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Priority Column -->
                    <ng-container matColumnDef="priority">
                      <th mat-header-cell *matHeaderCellDef>Priority</th>
                      <td mat-cell *matCellDef="let task">
                        <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                          {{ task.priority }}
                        </span>
                      </td>
                    </ng-container>

                    <!-- Completed At Column -->
                    <ng-container matColumnDef="dueDate">
                      <th mat-header-cell *matHeaderCellDef>Completed At</th>
                      <td mat-cell *matCellDef="let task" class="text-success font-semibold">
                        {{ task.completedAt | date:'mediumDate' }}
                        @if (task.dueDate && task.completedAt && (task.completedAt > task.dueDate)) {
                          <span style="font-size: 0.7rem; font-weight: normal;" class="badge badge-priority-critical ml-4">LATE</span>
                        }
                      </td>
                    </ng-container>

                    <tr mat-headerRowDef="displayedColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="interactive-row"></tr>
                  </table>
                  <mat-paginator #completedPaginator
                                 [length]="completedTotalCount()"
                                 [pageSize]="pageSize()"
                                 [pageSizeOptions]="[5, 10, 20]"
                                 (page)="onPageChange($event)">
                  </mat-paginator>
                }
              </div>
            </mat-tab>
          </mat-tab-group>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .reports-view {
      position: relative;
      display: flex;
      flex-direction: column;
      gap: 0px;
    }

    .loading-overlay {
      position: absolute;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(255,255,255,0.7);
      backdrop-filter: blur(4px);
      z-index: 1000;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      gap: 16px;
      border-radius: 16px;
    }
    .loading-overlay p {
      font-size: 0.95rem;
      font-weight: 500;
      color: var(--primary);
    }

    /* Filters Box Custom Styling */
    .filters-card {
      border: 1px solid var(--border);
      background: rgba(255, 255, 255, 0.8) !important;
      backdrop-filter: var(--backdrop-blur);
    }
    .filter-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }
    .filter-item {
      width: 100%;
    }
    
    /* Material fields styling overrides */
    ::ng-deep .filters-card .mat-mdc-text-field-wrapper {
      background-color: rgba(248, 250, 252, 0.6) !important;
      border-radius: 12px !important;
      transition: all var(--transition-fast) !important;
    }
    ::ng-deep .filters-card .mat-mdc-text-field-wrapper:hover {
      background-color: rgba(255, 255, 255, 0.9) !important;
    }
    ::ng-deep .filters-card .mat-mdc-form-field.mat-focused .mat-mdc-text-field-wrapper {
      background-color: #ffffff !important;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12) !important;
    }
    
    /* Correctly style outline borders to prevent floating label overlap */
    ::ng-deep .filters-card .mdc-notched-outline__leading,
    ::ng-deep .filters-card .mdc-notched-outline__notch,
    ::ng-deep .filters-card .mdc-notched-outline__trailing {
      border-color: var(--border) !important;
      transition: border-color var(--transition-fast) !important;
    }
    ::ng-deep .filters-card .mdc-notched-outline__leading {
      border-top-left-radius: 12px !important;
      border-bottom-left-radius: 12px !important;
    }
    ::ng-deep .filters-card .mdc-notched-outline__trailing {
      border-top-right-radius: 12px !important;
      border-bottom-right-radius: 12px !important;
    }
    
    /* Hover and focus states on outline borders */
    ::ng-deep .filters-card .mat-mdc-form-field-flex:hover .mdc-notched-outline__leading,
    ::ng-deep .filters-card .mat-mdc-form-field-flex:hover .mdc-notched-outline__notch,
    ::ng-deep .filters-card .mat-mdc-form-field-flex:hover .mdc-notched-outline__trailing {
      border-color: #cbd5e1 !important;
    }
    ::ng-deep .filters-card .mat-mdc-form-field.mat-focused .mdc-notched-outline__leading,
    ::ng-deep .filters-card .mat-mdc-form-field.mat-focused .mdc-notched-outline__notch,
    ::ng-deep .filters-card .mat-mdc-form-field.mat-focused .mdc-notched-outline__trailing {
      border-color: var(--primary) !important;
      border-width: 2px !important;
    }
    
    ::ng-deep .filters-card .mat-mdc-form-field-subscript-wrapper {
      display: none !important;
    }
    ::ng-deep .filters-card .mat-mdc-form-field-prefix {
      display: inline-flex;
      align-items: center;
      align-self: center;
    }

    /* Error banner */
    .error-banner {
      background: var(--danger-bg);
      color: var(--danger);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 12px;
      padding: 16px;
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .error-message {
      flex-grow: 1;
    }
    .error-message h4 {
      margin-bottom: 2px;
      color: var(--danger);
    }
    .error-message p {
      font-size: 0.85rem;
      margin: 0;
    }

    /* KPI Grid Custom Styling */
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
      gap: 20px;
    }
    .kpi-card {
      padding: 20px 24px;
      border: 1px solid var(--border);
      border-radius: 16px;
      background: rgba(255, 255, 255, 0.85) !important;
      transition: all var(--transition-normal) !important;
      cursor: pointer;
    }
    .kpi-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 12px 24px -10px rgba(99, 102, 241, 0.12), var(--shadow-lg) !important;
      border-color: rgba(99, 102, 241, 0.25) !important;
    }
    .kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 6px;
    }
    .kpi-value {
      line-height: 1;
    }
    .kpi-icon-bg {
      width: 48px;
      height: 48px;
      border-radius: 14px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.02);
      transition: all var(--transition-normal);
    }
    .kpi-card:hover .kpi-icon-bg {
      transform: scale(1.08) rotate(3deg);
    }
    
    .bg-primary-light {
      background-color: #f5f3ff !important;
      color: #7c3aed !important;
    }
    .bg-success-light {
      background-color: #ecfdf5 !important;
      color: #10b981 !important;
    }
    .bg-danger-light {
      background-color: #fef2f2 !important;
      color: #ef4444 !important;
    }

    .kpi-progress-circle {
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .circular-chart {
      max-height: 48px;
    }
    .circle-bg {
      stroke: #f1f5f9;
    }
    .circle {
      stroke: var(--secondary);
      transition: stroke-dasharray 0.6s ease;
    }

    /* Charts Section */
    .charts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 24px;
    }
    .chart-card {
      min-height: 270px;
      display: flex;
      flex-direction: column;
    }
    mat-card-title {
      font-size: 1rem !important;
      font-weight: 600 !important;
    }

    /* Donut chart styles */
    .svg-container {
      flex-shrink: 0;
    }
    .donut-center-text {
      position: absolute;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      display: flex;
      flex-direction: column;
      align-items: center;
      line-height: 1;
    }
    .donut-center-text strong {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--text-main);
    }
    .donut-center-text span {
      font-size: 0.7rem;
      color: var(--text-muted);
      text-transform: uppercase;
      font-weight: 600;
    }
    .legend-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 140px;
    }
    .legend-item {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.8rem;
    }
    .legend-dot {
      width: 10px; height: 10px;
      border-radius: 50%;
      flex-shrink: 0;
    }
    .legend-label {
      color: var(--text-muted);
      flex-grow: 1;
    }
    .legend-count {
      font-weight: 600;
      color: var(--text-main);
    }

    /* Priority chart styles */
    .priority-chart-container {
      display: flex;
      align-items: flex-end;
      justify-content: space-around;
      height: 140px;
      border-bottom: 1.5px solid var(--border);
      padding-bottom: 2px;
      margin-bottom: 8px;
    }
    .priority-bar-wrapper {
      display: flex;
      flex-direction: column;
      align-items: center;
      width: 60px;
    }
    .bar-count {
      font-size: 0.75rem;
      font-weight: 600;
      margin-bottom: 4px;
    }
    .priority-bar {
      width: 24px;
      border-radius: 4px 4px 0 0;
      transition: height 0.5s cubic-bezier(0.4, 0, 0.2, 1);
      cursor: pointer;
    }
    .priority-bar:hover {
      filter: brightness(0.9);
    }
    .bar-label {
      font-size: 0.7rem;
      font-weight: 500;
      color: var(--text-muted);
      margin-top: 8px;
    }

    /* Assignee stats styles */
    .assignee-chart-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .assignee-stat-row {
      display: flex;
      flex-direction: column;
    }
    .progress-bar-bg {
      height: 8px;
      background-color: #f1f5f9;
      border-radius: 9999px;
      overflow: hidden;
      display: flex;
    }
    .progress-done {
      background-color: var(--success);
      height: 100%;
      transition: width 0.5s ease;
    }
    .progress-pending {
      background-color: var(--primary);
      height: 100%;
      transition: width 0.5s ease;
      opacity: 0.7;
    }
    .progress-overdue {
      background-color: var(--danger);
      height: 100%;
      transition: width 0.5s ease;
    }

    /* Lists card styles */
    .table-card {
      border: 1px solid var(--border);
      overflow: hidden;
    }
    ::ng-deep .mat-mdc-tab-group {
      --mdc-tab-indicator-active-indicator-color: var(--primary);
      --mat-tab-header-active-label-text-color: var(--primary);
    }
    .table-container {
      overflow-x: auto;
      min-height: 250px;
    }
    .reports-table {
      width: 100%;
      background: transparent;
    }
    .reports-table th {
      font-weight: 600;
      color: var(--text-muted);
      font-size: 0.8rem;
      text-transform: uppercase;
      border-bottom: 1px solid var(--border);
      padding: 12px 16px;
    }
    .reports-table td {
      padding: 12px 16px;
      border-bottom: 1px solid var(--border);
      font-size: 0.9rem;
    }
    .interactive-row:hover {
      background-color: rgba(99, 102, 241, 0.02);
    }
    .task-title-link {
      font-weight: 500;
      color: var(--text-main);
    }
    .task-title-link:hover {
      color: var(--primary);
    }

    /* Empty states */
    .empty-state {
      padding: 48px 16px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      color: var(--text-muted);
    }
    .empty-icon {
      font-size: 48px;
      width: 48px; height: 48px;
      margin-bottom: 16px;
    }
    .empty-state h3 {
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--text-main);
      margin-bottom: 6px;
    }
    .empty-state p {
      font-size: 0.85rem;
      max-width: 320px;
      margin: 0;
    }

    .no-data-msg {
      font-size: 0.9rem;
      color: var(--text-muted);
      text-align: center;
      padding: 32px 0;
    }
    
    .ml-4 { margin-left: 4px; }
    .mr-8 { margin-right: 8px; }
    .mt-16 { margin-top: 16px; }
    .pt-16 { padding-top: 16px; }
    .mb-12 { margin-bottom: 12px; }
    .mb-24 { margin-bottom: 24px; }
    .p-0 { padding: 0 !important; }
  `]
})
export class Reports implements OnInit, AfterViewInit {
  private readonly reportService = inject(ReportService);
  private readonly projectService = inject(ProjectService);
  private readonly userService = inject(UserService);
  private readonly toast = inject(ToastService);
  private readonly http = inject(HttpClient);

  @ViewChild('overduePaginator') overduePaginator!: MatPaginator;
  @ViewChild('uncompletedPaginator') uncompletedPaginator!: MatPaginator;
  @ViewChild('completedPaginator') completedPaginator!: MatPaginator;

  // Signals
  loading = signal(false);
  error = signal<string | null>(null);
  projects = signal<any[]>([]);
  allUsers = signal<any[]>([]);
  assignees = signal<any[]>([]);
  dynamicFieldsList = signal<any[]>([]);

  // Report results signals
  summary = signal<any>({
    totalTasks: 0,
    completedTasks: 0,
    overdueTasks: 0,
    soonDueTasks: 0,
    completedOnTime: 0,
    completedLate: 0,
    completionRate: 0,
    statusCounts: {},
    priorityCounts: {}
  });

  assigneeStats = signal<any[]>([]);
  projectStats = signal<any[]>([]);

  // Filters State
  filters: any = {
    projectId: null,
    assigneeId: null,
    status: null,
    priority: null,
    dateFrom: null,
    dateTo: null
  };

  dynamicFieldFilters: { [key: string]: string } = {};

  // Table Data Sources
  overdueDataSource = new MatTableDataSource<any>([]);
  uncompletedDataSource = new MatTableDataSource<any>([]);
  completedDataSource = new MatTableDataSource<any>([]);

  // Pagination totals
  overdueTotalCount = signal(0);
  uncompletedTotalCount = signal(0);
  completedTotalCount = signal(0);

  pageSize = signal(10);
  activeTab = signal(0); // 0: Overdue, 1: Uncompleted, 2: Completed

  displayedColumns: string[] = ['title', 'project', 'assignee', 'status', 'priority', 'dueDate'];

  // SVG Chart Segment computations
  statusSegments = computed(() => {
    const total = this.summary().totalTasks;
    if (total === 0) return [];

    let accumulatedPercent = 0;
    const counts = this.summary().statusCounts || {};
    const statuses = ['Todo', 'InProgress', 'InReview', 'Done', 'Cancelled'];

    return statuses
      .filter(key => counts[key] > 0)
      .map(key => {
        const count = counts[key] || 0;
        const percent = (count / total) * 100;
        const strokeLength = (percent / 100) * 314.159;
        const strokeOffset = -(accumulatedPercent / 100) * 314.159;
        accumulatedPercent += percent;

        return {
          key: this.formatStatus(key),
          count,
          percent,
          strokeLength,
          strokeOffset,
          color: this.getStatusColor(key)
        };
      });
  });

  priorityBars = computed(() => {
    const counts = this.summary().priorityCounts || {};
    const priorities = ['Low', 'Medium', 'High', 'Critical'];
    const maxVal = Math.max(...priorities.map(p => counts[p] || 0), 1);

    return priorities.map(key => {
      const count = counts[key] || 0;
      const height = (count / maxVal) * 100; // scale to max 100px
      return {
        key,
        count,
        height: Math.max(height, count > 0 ? 6 : 0), // min 6px height if count > 0
        color: this.getPriorityColor(key)
      };
    });
  });

  ngOnInit() {
    this.loadInitialData();
  }

  ngAfterViewInit() {
    this.overdueDataSource.paginator = this.overduePaginator;
    this.uncompletedDataSource.paginator = this.uncompletedPaginator;
    this.completedDataSource.paginator = this.completedPaginator;
  }

  loadInitialData() {
    this.loading.set(true);
    this.error.set(null);

    // Fetch projects and users
    this.projectService.getProjects(1, 100).subscribe({
      next: (res) => {
        this.projects.set(res.items || []);
        this.loadUsers();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set('Failed to load projects list.');
        this.toast.error('Could not load projects.');
      }
    });
  }

  loadUsers() {
    this.userService.getUsers(1, 100).subscribe({
      next: (res) => {
        const users = res.items || [];
        this.allUsers.set(users);
        this.assignees.set(users); // initial load
        this.refreshReports();
      },
      error: (err) => {
        // Fallback for non-admin users: aggregate members from all projects they belong to
        const projectsList = this.projects();
        if (projectsList.length === 0) {
          this.allUsers.set([]);
          this.assignees.set([]);
          this.refreshReports();
          return;
        }

        const memberRequests = projectsList.map(p => this.projectService.getMembers(p.id));
        forkJoin(memberRequests).subscribe({
          next: (results) => {
            const userMap = new Map<string, any>();
            results.forEach((mList: any) => {
              if (mList) {
                mList.forEach((m: any) => {
                  userMap.set(m.userId, {
                    id: m.userId,
                    fullName: m.fullName,
                    email: m.email
                  });
                });
              }
            });
            const users = Array.from(userMap.values());
            this.allUsers.set(users);
            this.assignees.set(users);
            this.refreshReports();
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Failed to load users list.');
            this.toast.error('Could not load assignees.');
          }
        });
      }
    });
  }

  onProjectChange() {
    // Clear project-specific assignee and dynamic fields
    this.filters.assigneeId = null;
    this.dynamicFieldFilters = {};
    this.dynamicFieldsList.set([]);

    if (this.filters.projectId) {
      // Load project-specific members
      this.projectService.getMembers(this.filters.projectId).subscribe({
        next: (res) => {
          const members = (res || []).map((m: any) => ({
            id: m.userId,
            fullName: m.userFullName
          }));
          this.assignees.set(members);
        },
        error: () => this.toast.error('Failed to load project members.')
      });

      // Fetch dynamic fields definition
      this.reportService.getAdvancedTasks({
        filter: {
          operator: 'AND',
          rules: [{ field: 'ProjectId', operator: 'Equals', value: this.filters.projectId }]
        },
        page: 1,
        pageSize: 1
      }).subscribe({
        next: () => {
          // Dynamic fields definition can be loaded from DynamicFieldService
          // Let's call the actual API endpoint /api/projects/{id}/dynamic-fields via HttpClient or inline
          // Let's fetch it using a simple inline fetch or register it in dynamic-field.service
          this.loadProjectDynamicFields(this.filters.projectId);
        }
      });
    } else {
      this.assignees.set(this.allUsers());
    }

    this.refreshReports();
  }

  private loadProjectDynamicFields(projectId: string) {
    this.http.get<any[]>(`http://localhost:5035/api/projects/${projectId}/dynamic-fields`).subscribe({
      next: (fields) => {
        this.dynamicFieldsList.set(fields || []);
      },
      error: () => this.toast.error('Failed to load project dynamic fields.')
    });
  }

  onDynamicFilterChange() {
    // Debounce dynamic fields filtering
    this.refreshReports();
  }

  refreshReports() {
    this.loading.set(true);
    this.error.set(null);

    // Merge standard filters with dynamic fields filters
    const queryFilters = {
      ...this.filters,
      dynamicFields: this.dynamicFieldFilters
    };

    // Load stats summary
    this.reportService.getWorkSummary(queryFilters).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loadChartsData(queryFilters);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Error occurred while fetching work summary.');
      }
    });
  }

  loadChartsData(queryFilters: any) {
    // Status stats
    this.reportService.getTasksByAssignee(queryFilters).subscribe({
      next: (assignees) => {
        this.assigneeStats.set(assignees || []);
      }
    });

    this.reportService.getTasksByProject(queryFilters).subscribe({
      next: (projects) => {
        this.projectStats.set(projects || []);
      }
    });

    // Load active tab task list
    this.loadActiveTabList(queryFilters);
  }

  loadActiveTabList(queryFilters: any) {
    const page = 1;
    const size = this.pageSize();

    if (this.activeTab() === 0) {
      this.reportService.getOverdueTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.overdueDataSource.data = res.items || [];
          this.overdueTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else if (this.activeTab() === 1) {
      this.reportService.getUncompletedTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.uncompletedDataSource.data = res.items || [];
          this.uncompletedTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else {
      this.reportService.getCompletedTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.completedDataSource.data = res.items || [];
          this.completedTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  onTabChange(index: number) {
    this.activeTab.set(index);
    this.loading.set(true);
    const queryFilters = {
      ...this.filters,
      dynamicFields: this.dynamicFieldFilters
    };
    this.loadActiveTabList(queryFilters);
  }

  onPageChange(event: any) {
    const page = event.pageIndex + 1;
    const size = event.pageSize;
    this.pageSize.set(size);
    this.loading.set(true);

    const queryFilters = {
      ...this.filters,
      dynamicFields: this.dynamicFieldFilters
    };

    if (this.activeTab() === 0) {
      this.reportService.getOverdueTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.overdueDataSource.data = res.items || [];
          this.overdueTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else if (this.activeTab() === 1) {
      this.reportService.getUncompletedTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.uncompletedDataSource.data = res.items || [];
          this.uncompletedTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else {
      this.reportService.getCompletedTasks(queryFilters, page, size).subscribe({
        next: (res) => {
          this.completedDataSource.data = res.items || [];
          this.completedTotalCount.set(res.totalCount || 0);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  // Helpers
  isOverdue(dueDate: any): boolean {
    if (!dueDate) return false;
    return new Date(dueDate) < new Date();
  }

  formatStatus(status: string): string {
    if (status === 'InProgress') return 'In Progress';
    if (status === 'InReview') return 'In Review';
    return status;
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Todo': return '#64748b'; // Slate
      case 'InProgress': return '#3b82f6'; // Blue
      case 'InReview': return '#8b5cf6'; // Purple
      case 'Done': return '#10b981'; // Emerald
      case 'Cancelled': return '#ef4444'; // Red
      default: return '#94a3b8';
    }
  }

  getPriorityColor(priority: string): string {
    switch (priority) {
      case 'Low': return '#10b981'; // Green
      case 'Medium': return '#f59e0b'; // Amber
      case 'High': return '#ef4444'; // Red
      case 'Critical': return '#7f1d1d'; // Dark Red
      default: return '#94a3b8';
    }
  }

  resetFilters() {
    this.filters = {
      projectId: null,
      assigneeId: null,
      status: null,
      priority: null,
      dateFrom: null,
      dateTo: null
    };
    this.dynamicFieldFilters = {};
    this.assignees.set(this.allUsers());
    this.refreshReports();
  }
}

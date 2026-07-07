import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ProjectService } from '../../core/services/project.service';
import { TaskService } from '../../core/services/task.service';
import { AuditLogService } from '../../core/services/audit-log.service';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { DynamicFieldService } from '../../core/services/dynamic-field.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { ConfirmDialog } from '../../shared/components/confirm-dialog';
import { Avatar } from '../../shared/components/avatar';
import { TaskDetailModal } from '../tasks/task-detail-modal';
@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner, ConfirmDialog, Avatar, TaskDetailModal],
  template: `
    <div class="detail-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>
      <app-confirm-dialog
        [active]="showConfirmDelete()"
        title="Delete Project"
        message="Are you sure you want to delete this project? This will soft-delete the project and hide it from all members."
        (confirm)="onDeleteConfirm()"
        (cancel)="onDeleteCancel()"
      ></app-confirm-dialog>

      <!-- Project Header Card -->
      @if (project()) {
        <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
          <div class="project-header-left">
            <span class="badge mb-8" [ngClass]="'badge-status-' + project().status.toLowerCase()">
              {{ project().status }}
            </span>
            <h2 class="project-name">{{ project().name }}</h2>
            <p class="project-desc">{{ project().description || 'No description provided.' }}</p>
          </div>
          
          <div class="project-header-right flex align-center gap-12">
            @if (canEditProject()) {
              <button class="btn btn-outline" (click)="openEditModal()">
                <span class="material-symbols-rounded">edit</span>
                Edit Project
              </button>
            }
            @if (canDeleteProject()) {
              <button class="btn btn-danger" (click)="triggerDeleteProject()">
                <span class="material-symbols-rounded">delete</span>
                Delete
              </button>
            }
          </div>
        </div>
      }

      <!-- Tab Navigation -->
      <div class="tabs-container mb-24">
        <button class="tab-btn" [ngClass]="{ 'active': activeTab() === 'tasks' }" (click)="setTab('tasks')">
          <span class="material-symbols-rounded">assignment</span>
          Tasks
        </button>
        <button class="tab-btn" [ngClass]="{ 'active': activeTab() === 'members' }" (click)="setTab('members')">
          <span class="material-symbols-rounded">group</span>
          Members
        </button>
        <button class="tab-btn" [ngClass]="{ 'active': activeTab() === 'gantt' }" (click)="setTab('gantt')">
          <span class="material-symbols-rounded">date_range</span>
          Gantt Chart
        </button>
        @if (canViewAuditLogs()) {
          <button class="tab-btn" [ngClass]="{ 'active': activeTab() === 'audit' }" (click)="setTab('audit')">
            <span class="material-symbols-rounded">history</span>
            Audit Logs
          </button>
        }
        @if (canManageDynamicFields()) {
          <button class="tab-btn" [ngClass]="{ 'active': activeTab() === 'dynamic-fields' }" (click)="setTab('dynamic-fields')">
            <span class="material-symbols-rounded">settings_applications</span>
            Dynamic Fields
          </button>
        }
      </div>

      <!-- Tab Contents -->
      <div class="tab-content animate-fade-in">
        <!-- 1. TASKS TAB -->
        @if (activeTab() === 'tasks') {
          <div class="tasks-tab">
            <!-- Filter Bar -->
            <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
              <div class="filters-left flex align-center gap-16 flex-wrap">
                <input 
                  type="text" 
                  class="form-input search-input" 
                  placeholder="Search tasks..." 
                  [(ngModel)]="taskFilters.search"
                  (input)="onTaskFilterChange()"
                />
                <select class="form-select" [(ngModel)]="taskFilters.status" (change)="onTaskFilterChange()">
                  <option value="">All Statuses</option>
                  <option value="Todo">Todo</option>
                  <option value="InProgress">In Progress</option>
                  <option value="InReview">In Review</option>
                  <option value="Done">Done</option>
                  <option value="Cancelled">Cancelled</option>
                </select>
                <select class="form-select" [(ngModel)]="taskFilters.priority" (change)="onTaskFilterChange()">
                  <option value="">All Priorities</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              
              <div class="flex align-center gap-12 flex-wrap">
                <!-- View Mode Toggle -->
                <div class="toggle-group flex p-4 rounded-8" style="background-color: #f1f5f9; gap: 4px;">
                  <button 
                    type="button"
                    class="btn btn-sm btn-text flex align-center gap-4" 
                    [ngClass]="{ 'active-toggle': viewMode() === 'kanban' }"
                    (click)="viewMode.set('kanban')"
                    style="padding: 6px 12px; font-size: 0.85rem;"
                  >
                    <span class="material-symbols-rounded" style="font-size: 16px;">dashboard</span>
                    Board
                  </button>
                  <button 
                    type="button"
                    class="btn btn-sm btn-text flex align-center gap-4" 
                    [ngClass]="{ 'active-toggle': viewMode() === 'table' }"
                    (click)="viewMode.set('table')"
                    style="padding: 6px 12px; font-size: 0.85rem;"
                  >
                    <span class="material-symbols-rounded" style="font-size: 16px;">format_list_bulleted</span>
                    List
                  </button>
                </div>

                @if (canCreateTask()) {
                  <button class="btn btn-primary" (click)="openCreateTaskModal()">
                    <span class="material-symbols-rounded">add</span>
                    Create Task
                  </button>
                }
              </div>
            </div>

            <!-- Kanban Board -->
            @if (viewMode() === 'kanban') {
              <div class="kanban-board">
                @for (col of kanbanColumns; track col.status) {
                  <div class="kanban-col">
                    <div class="kanban-col-header">
                      <h4>{{ col.name }}</h4>
                      <span class="task-count">{{ getTaskCountInCol(col.status) }}</span>
                    </div>
                    
                    <div 
                      class="kanban-col-cards"
                      [class.drag-over]="activeDragOverStatus() === col.status"
                      (dragover)="onDragOver($event)"
                      (dragenter)="onDragEnter($event, col.status)"
                      (dragleave)="onDragLeave($event)"
                      (drop)="onDrop($event, col.status)"
                    >
                      @for (task of getTasksByStatus(col.status); track task.id) {
                        <div 
                          class="kanban-card" 
                          [class.dragging]="draggedTask()?.id === task.id"
                          draggable="true"
                          (dragstart)="onDragStart($event, task)"
                          (dragend)="onDragEnd($event)"
                          (click)="openTaskDetails(task.id)"
                        >
                          <div class="card-header flex justify-between align-center mb-8">
                            <span class="badge" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">
                              {{ task.priority }}
                            </span>
                            <button class="btn btn-text text-muted" (click)="openTaskDetailsAndEdit(task.id); $event.stopPropagation()" title="Edit Task" style="padding: 0; min-width: auto; height: auto;">
                              <span class="material-symbols-rounded" style="font-size: 16px;">edit</span>
                            </button>
                          </div>
                          <h5 class="task-title mb-8">{{ task.title }}</h5>
                          <div class="flex gap-4 flex-wrap align-center mb-8">
                            <!-- Parent Task Indicator -->
                            @if (task.parentTaskTitle) {
                              <div class="parent-task-badge" style="display: inline-flex; align-items: center; gap: 4px; font-size: 0.7rem; color: var(--text-muted); background-color: #f1f5f9; padding: 2px 6px; border-radius: 4px;">
                                <span class="material-symbols-rounded" style="font-size: 12px; font-weight: bold;">subdirectory_arrow_right</span>
                                <span style="max-width: 140px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" [title]="task.parentTaskTitle">{{ task.parentTaskTitle }}</span>
                              </div>
                            }

                            <!-- Subtask Progress Indicator -->
                            @if (task.subtasksCount > 0) {
                              <div class="subtask-progress" style="display: inline-flex; align-items: center; gap: 4px; font-size: 0.7rem; color: #4f46e5; background-color: #e0e7ff; padding: 2px 6px; border-radius: 4px; font-weight: 600;">
                                <span class="material-symbols-rounded" style="font-size: 12px;">account_tree</span>
                                <span>{{ task.completedSubtasksCount }}/{{ task.subtasksCount }} subtasks</span>
                              </div>
                            }
                          </div>
                          @if (task.description) {
                            <p class="task-desc">{{ task.description }}</p>
                          }
                          <div class="card-footer mt-12 pt-12 flex justify-between align-center">
                            <div class="assignee flex align-center gap-8">
                              <app-avatar [name]="task.assigneeName || 'Unassigned'" [size]="24"></app-avatar>
                              <span class="assignee-name">{{ task.assigneeName || 'Unassigned' }}</span>
                            </div>
                            @if (task.dueDate) {
                              <span class="due-date" [ngClass]="{ 'overdue': isOverdue(task.dueDate) && col.status !== 'Done' }">
                                {{ task.dueDate | date:'mediumDate' }}
                              </span>
                            }
                          </div>
                        </div>
                      }

                      <!-- Dotted drop placeholder like in Jira -->
                      @if (draggedTask() && draggedTask()?.status !== col.status && activeDragOverStatus() === col.status) {
                        <div class="drag-placeholder">
                          Drop here to change status
                        </div>
                      }
                    </div>
                  </div>
                }
              </div>
            }

            <!-- Table List View -->
            @if (viewMode() === 'table') {
              <div class="glass-card animate-fade-in">
                <div class="responsive-table-container">
                  <table class="responsive-table">
                    <thead>
                      <tr>
                        <th>Task Title</th>
                        <th>Parent Task</th>
                        <th>Assignee</th>
                        <th>Priority</th>
                        <th>Status</th>
                        <th>Due Date</th>
                        <th class="text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      @if (tasks().length === 0) {
                        <tr>
                          <td colspan="7" class="text-center py-24 text-muted">
                            No tasks found matching your filters.
                          </td>
                        </tr>
                      } @else {
                        @for (task of tasks(); track task.id) {
                          <tr class="clickable-row" (click)="openTaskDetails(task.id)">
                            <td>
                              <div class="task-title-cell" style="font-weight: 600;">{{ task.title }}</div>
                            </td>
                            <td>
                              @if (task.parentTaskTitle) {
                                <span class="badge badge-status-todo" style="display: inline-flex; align-items: center; gap: 4px; font-size: 0.75rem;">
                                  <span class="material-symbols-rounded" style="font-size: 14px;">subdirectory_arrow_right</span>
                                  {{ task.parentTaskTitle }}
                                </span>
                              } @else {
                                <span class="text-light">-</span>
                              }
                            </td>
                            <td>
                              <div class="flex align-center gap-8">
                                <app-avatar [name]="task.assigneeName || 'Unassigned'" [size]="24"></app-avatar>
                                <span style="font-size: 0.85rem;">{{ task.assigneeName || 'Unassigned' }}</span>
                              </div>
                            </td>
                            <td>
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
                                <span style="font-size: 0.85rem;" [ngClass]="{ 'overdue': isOverdue(task.dueDate) && task.status !== 'Done' }">
                                  {{ task.dueDate | date:'mediumDate' }}
                                </span>
                              } @else {
                                <span class="text-light">-</span>
                              }
                            </td>
                            <td class="text-right" (click)="$event.stopPropagation()">
                              <button class="btn btn-text text-muted" (click)="openTaskDetailsAndEdit(task.id)" title="Edit Task" style="padding: 4px; min-width: auto;">
                                <span class="material-symbols-rounded" style="font-size: 18px;">edit</span>
                              </button>
                            </td>
                          </tr>
                        }
                      }
                    </tbody>
                  </table>
                </div>
              </div>
            }
          </div>
        }

        <!-- 2. MEMBERS TAB -->
        @if (activeTab() === 'members') {
          <div class="members-tab">
            <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
              <h3>Project Members</h3>
              @if (canManageMembers()) {
                <button class="btn btn-primary" (click)="openAddMemberModal()">
                  <span class="material-symbols-rounded">person_add</span>
                  Add Member
                </button>
              }
            </div>

            <div class="glass-card">
              <div class="responsive-table-container">
                <table class="responsive-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Joined Date</th>
                      @if (canManageMembers()) {
                        <th>Actions</th>
                      }
                    </tr>
                  </thead>
                  <tbody>
                    @for (member of members(); track member.userId) {
                      <tr>
                        <td>
                          <div class="flex align-center gap-12">
                            <app-avatar [name]="member.fullName" [size]="36"></app-avatar>
                            <strong>{{ member.fullName }}</strong>
                          </div>
                        </td>
                        <td>{{ member.email }}</td>
                        <td>
                          @if (canManageMembers() && member.userId !== project()?.ownerId) {
                            <select 
                              class="form-select inline-select" 
                              [value]="member.roleInProject" 
                              (change)="changeMemberRole(member.userId, $event)"
                            >
                              <option value="ProjectManager">Project Manager</option>
                              <option value="Member">Member</option>
                            </select>
                          } @else {
                            <span class="badge badge-status-inprogress">
                              {{ member.roleInProject === 'ProjectManager' ? 'Project Manager' : member.roleInProject }}
                            </span>
                          }
                        </td>
                        <td>{{ member.joinedAt | date:'mediumDate' }}</td>
                        @if (canManageMembers()) {
                          <td>
                            @if (member.userId !== project()?.ownerId) {
                              <button class="btn btn-text text-danger-color" (click)="removeMember(member.userId)">
                                <span class="material-symbols-rounded">person_remove</span>
                              </button>
                            } @else {
                              <span class="owner-indicator">Owner</span>
                            }
                          </td>
                        }
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        }

        <!-- 3. AUDIT LOGS TAB -->
        @if (activeTab() === 'audit') {
          <div class="audit-tab glass-card">
            <h3 class="mb-16">History Logs</h3>
            <div class="audit-history">
              @if (auditLogs().length === 0) {
                <div class="text-center py-24 text-muted">No change logs recorded.</div>
              } @else {
                @for (log of auditLogs(); track log.id) {
                  <div class="audit-row animate-fade-in-up">
                    <span class="material-symbols-rounded log-type-icon">
                      {{ getAuditIcon(log.entityType) }}
                    </span>
                    <div class="log-details">
                      <div class="log-title">
                        <strong>{{ log.changedByName }}</strong>
                        {{ getAuditActionLabel(log.action) }}
                        {{ log.entityType.toLowerCase() }}
                        <strong>{{ log.entityId }}</strong>
                      </div>
                      <div class="log-time">{{ log.changedAt | date:'medium' }}</div>
                      @if (log.oldValue || log.newValue) {
                        <div class="log-diff">
                          @if (log.oldValue) {
                            <div class="diff-old">Before: {{ log.oldValue }}</div>
                          }
                          @if (log.newValue) {
                            <div class="diff-new">After: {{ log.newValue }}</div>
                          }
                        </div>
                      }
                    </div>
                  </div>
                }
              }
            </div>
          </div>
        }

        <!-- 4. GANTT CHART TAB -->
        @if (activeTab() === 'gantt') {
          <div class="gantt-tab glass-card">
            <div class="flex justify-between align-center mb-16 flex-wrap gap-8">
              <h3>Project Gantt Timeline</h3>
              @if (timelineDays().length > 0) {
                <div class="text-muted" style="font-size: 0.85rem;">
                  Displaying {{ tasks().length }} tasks from {{ timelineDays()[0] | date:'mediumDate' }} to {{ timelineDays()[timelineDays().length - 1] | date:'mediumDate' }}
                </div>
              }
            </div>

            @if (tasks().length === 0) {
              <div class="text-center py-48 text-muted">
                No tasks available in this project to display on the timeline.
              </div>
            } @else {
              <div class="gantt-wrapper">
                <!-- Task Sidebar -->
                <div class="gantt-sidebar">
                  <div class="gantt-sidebar-header">Task Title</div>
                  <div class="gantt-sidebar-rows">
                    @for (task of tasks(); track task.id) {
                      <div class="gantt-sidebar-row" (click)="openTaskDetails(task.id)">
                        <div class="gantt-sidebar-title" [title]="task.title">{{ task.title }}</div>
                        <div class="gantt-sidebar-meta">
                          <span class="badge badge-sm" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">{{ task.priority }}</span>
                          <span class="assignee-name" style="font-size: 0.75rem;">{{ task.assigneeName || 'Unassigned' }}</span>
                        </div>
                      </div>
                    }
                  </div>
                </div>

                <!-- Timeline Grid (Scrollable) -->
                <div class="gantt-timeline-container">
                  <div class="gantt-timeline-header" [style.grid-template-columns]="'repeat(' + timelineDays().length + ', 45px)'">
                    @for (day of timelineDays(); track day.getTime()) {
                      <div class="gantt-timeline-header-cell" [class.is-today]="isToday(day)">
                        <div class="day-month">{{ day | date:'MMM' }}</div>
                        <div class="day-num">{{ day | date:'d' }}</div>
                        <div class="day-name">{{ day | date:'EEE' }}</div>
                      </div>
                    }
                  </div>

                  <div class="gantt-timeline-rows">
                    @for (task of tasks(); track task.id) {
                      <div class="gantt-timeline-row" [style.grid-template-columns]="'repeat(' + timelineDays().length + ', 45px)'">
                        @for (day of timelineDays(); track day.getTime()) {
                          <div class="gantt-grid-cell" [class.is-today]="isToday(day)"></div>
                        }

                        @if (getTaskBarPosition(task)) {
                          <div 
                            class="gantt-bar-wrapper"
                            [style.grid-column]="getTaskBarPosition(task)"
                            (click)="openTaskDetails(task.id)"
                          >
                            <div class="gantt-bar" [ngClass]="'gantt-bar-' + task.status.toLowerCase()">
                              <span class="gantt-bar-title">{{ task.title }}</span>
                              
                              <!-- Premium Hover Tooltip -->
                              <div class="gantt-bar-tooltip">
                                <div class="tooltip-title">{{ task.title }}</div>
                                <div class="tooltip-detail">
                                  <span>Status:</span> 
                                  <span class="badge badge-sm" [ngClass]="'badge-status-' + task.status.toLowerCase()">{{ getStatusLabel(task.status) }}</span>
                                </div>
                                <div class="tooltip-detail">
                                  <span>Priority:</span>
                                  <span class="badge badge-sm" [ngClass]="'badge-priority-' + task.priority.toLowerCase()">{{ task.priority }}</span>
                                </div>
                                <div class="tooltip-detail"><span>Assignee:</span> {{ task.assigneeName || 'Unassigned' }}</div>
                                <div class="tooltip-detail"><span>Start Date:</span> {{ task.createdAt | date:'mediumDate' }}</div>
                                <div class="tooltip-detail"><span>Due Date:</span> {{ task.dueDate ? (task.dueDate | date:'mediumDate') : 'No due date' }}</div>
                              </div>
                            </div>
                          </div>
                        }
                      </div>
                    }
                  </div>
                </div>
              </div>
            }
          </div>
        }

        <!-- 5. DYNAMIC FIELDS TAB -->
        @if (activeTab() === 'dynamic-fields' && canManageDynamicFields()) {
          <div class="dynamic-fields-tab">
            <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
              <div>
                <h3>Dynamic Fields Definition</h3>
                <p class="text-muted" style="margin: 4px 0 0 0; font-size: 0.85rem;">Define custom fields for tasks in this project.</p>
              </div>
              <button class="btn btn-primary" (click)="openAddDynamicFieldModal()">
                <span class="material-symbols-rounded">add</span>
                Add Dynamic Field
              </button>
            </div>

            <div class="glass-card p-0">
              <div class="responsive-table-container">
                <table class="responsive-table">
                  <thead>
                    <tr>
                      <th>Field Name</th>
                      <th>Field Key</th>
                      <th>Type</th>
                      <th>Required</th>
                      <th>Options</th>
                      <th>Default Value</th>
                      <th>Display Order</th>
                      <th>Status</th>
                      <th class="text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    @if (dynamicFields().length === 0) {
                      <tr>
                        <td colspan="9" class="text-center py-24 text-muted">
                          No dynamic fields defined yet.
                        </td>
                      </tr>
                    } @else {
                      @for (field of dynamicFields(); track field.id) {
                        <tr>
                          <td><strong>{{ field.fieldName }}</strong></td>
                          <td><code>{{ field.fieldKey }}</code></td>
                          <td>
                            <span class="badge badge-status-todo" style="background-color: #f1f5f9; color: #334155;">
                              {{ field.fieldType }}
                            </span>
                          </td>
                          <td>
                            @if (field.isRequired) {
                              <span class="badge" style="background-color: #fee2e2; color: #ef4444;">Required</span>
                            } @else {
                              <span class="text-muted" style="font-size: 0.85rem;">Optional</span>
                            }
                          </td>
                          <td>
                            @if (field.options && field.options.length > 0) {
                              <div class="flex flex-wrap gap-4" style="max-width: 200px;">
                                @for (opt of field.options; track opt) {
                                  <span class="badge" style="background-color: #f1f5f9; color: #475569; font-size: 0.7rem; padding: 2px 6px;">{{ opt }}</span>
                                }
                              </div>
                            } @else {
                              <span class="text-muted">-</span>
                            }
                          </td>
                          <td>
                            @if (field.defaultValue) {
                              <code style="font-size: 0.8rem;">{{ field.defaultValue }}</code>
                            } @else {
                              <span class="text-muted">-</span>
                            }
                          </td>
                          <td>{{ field.displayOrder }}</td>
                          <td>
                            @if (field.isActive) {
                              <span class="badge badge-status-active" style="background-color: #dcfce7; color: #15803d;">Active</span>
                            } @else {
                              <span class="badge" style="background-color: #f1f5f9; color: #94a3b8;">Inactive</span>
                            }
                          </td>
                          <td class="text-right">
                            <button class="btn btn-text text-muted" (click)="openEditDynamicFieldModal(field)" title="Edit Field" style="padding: 4px; min-width: auto; margin-right: 8px;">
                              <span class="material-symbols-rounded" style="font-size: 18px;">edit</span>
                            </button>
                            <button class="btn btn-text text-danger" (click)="deleteDynamicField(field.id)" title="Delete Field" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded" style="font-size: 18px;">delete</span>
                            </button>
                          </td>
                        </tr>
                      }
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        }
      </div>

      <!-- Task Detail Modal Drawer -->
      @if (selectedTaskId()) {
        <app-task-detail-modal
          [taskId]="selectedTaskId()!"
          [startInEditMode]="editOnOpen()"
          (close)="closeTaskDetails()"
          (taskUpdated)="onTaskUpdated()"
        ></app-task-detail-modal>
      }

      <!-- Edit Project Modal -->
      @if (showEditModal()) {
        <div class="modal-overlay" (click)="closeEditModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Edit Project</h3>
              <button class="close-btn" (click)="closeEditModal()">&times;</button>
            </div>
            
            <form #editForm="ngForm" (ngSubmit)="onEditSubmit(editForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="edit-proj-name">Project Name</label>
                  <input 
                    type="text" 
                    id="edit-proj-name" 
                    name="name" 
                    class="form-input" 
                    [(ngModel)]="editProjectData.name" 
                    required 
                    maxlength="200"
                  />
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="edit-proj-desc">Description</label>
                  <textarea 
                    id="edit-proj-desc" 
                    name="description" 
                    class="form-input form-textarea" 
                    [(ngModel)]="editProjectData.description" 
                    maxlength="2000"
                  ></textarea>
                </div>

                <div class="form-group">
                  <label class="form-label" for="edit-proj-status">Status</label>
                  <select class="form-select" id="edit-proj-status" name="status" [(ngModel)]="editProjectData.status">
                    <option value="Active">Active</option>
                    <option value="Archived">Archived</option>
                  </select>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeEditModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="editForm.invalid || updatingProject()">
                  {{ updatingProject() ? 'Saving...' : 'Save Changes' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Create Task Modal -->
      @if (showCreateTaskModal()) {
        <div class="modal-overlay" (click)="closeCreateTaskModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Create New Task</h3>
              <button class="close-btn" (click)="closeCreateTaskModal()">&times;</button>
            </div>
            
            <form #taskForm="ngForm" (ngSubmit)="onCreateTaskSubmit(taskForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="task-title">Task Title</label>
                  <input 
                    type="text" 
                    id="task-title" 
                    name="title" 
                    class="form-input" 
                    [(ngModel)]="newTaskData.title" 
                    #taskTitle="ngModel" 
                    required 
                    maxlength="200"
                    placeholder="e.g. Implement authentication layout"
                  />
                </div>
                
                <div class="form-group">
                  <label class="form-label" for="task-desc">Description</label>
                  <textarea 
                    id="task-desc" 
                    name="description" 
                    class="form-input form-textarea" 
                    [(ngModel)]="newTaskData.description" 
                    maxlength="5000"
                    placeholder="Describe details of the task..."
                  ></textarea>
                </div>

                <div class="form-group">
                  <label class="form-label" for="task-priority">Priority</label>
                  <select class="form-select" id="task-priority" name="priority" [(ngModel)]="newTaskData.priority">
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                    <option value="Critical">Critical</option>
                  </select>
                </div>

                <div class="form-group">
                  <label class="form-label" for="task-assignee">Assignee</label>
                  <select class="form-select" id="task-assignee" name="assigneeId" [(ngModel)]="newTaskData.assigneeId">
                    <option [value]="null">Unassigned</option>
                    @for (m of members(); track m.userId) {
                      <option [value]="m.userId">{{ m.fullName }}</option>
                    }
                  </select>
                </div>

                <div class="form-group">
                  <label class="form-label" for="task-due">Due Date</label>
                  <input 
                    type="date" 
                    id="task-due" 
                    name="dueDate" 
                    class="form-input" 
                    [(ngModel)]="newTaskData.dueDate"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="task-parent">Parent Task</label>
                  <select class="form-select" id="task-parent" name="parentTaskId" [(ngModel)]="newTaskData.parentTaskId">
                    <option [value]="null">No Parent Task</option>
                    @for (t of tasks(); track t.id) {
                      <option [value]="t.id">{{ t.title }}</option>
                    }
                  </select>
                </div>

                <!-- Dynamic Fields -->
                @for (field of dynamicFields(); track field.id) {
                  @if (field.isActive) {
                    <div class="form-group">
                      <label class="form-label" for="create-df-{{field.fieldKey}}">
                        {{ field.fieldName }}
                        @if (field.isRequired) {
                          <span style="color: var(--danger);">*</span>
                        }
                      </label>

                      <!-- Text type -->
                      @if (field.fieldType === 'Text') {
                        <input
                          type="text"
                          id="create-df-{{field.fieldKey}}"
                          [name]="'df_' + field.fieldKey"
                          class="form-input"
                          [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                          [required]="field.isRequired"
                          #dfRef="ngModel"
                        />
                        @if (dfRef.invalid && (dfRef.dirty || dfRef.touched)) {
                          <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">{{ field.fieldName }} is required.</span>
                        }
                      }

                      <!-- Number type -->
                      @else if (field.fieldType === 'Number') {
                        <input
                          type="number"
                          id="create-df-{{field.fieldKey}}"
                          [name]="'df_' + field.fieldKey"
                          class="form-input"
                          [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                          [required]="field.isRequired"
                          #dfRef="ngModel"
                        />
                        @if (dfRef.invalid && (dfRef.dirty || dfRef.touched)) {
                          <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">{{ field.fieldName }} is required.</span>
                        }
                      }

                      <!-- Date type -->
                      @else if (field.fieldType === 'Date') {
                        <input
                          type="date"
                          id="create-df-{{field.fieldKey}}"
                          [name]="'df_' + field.fieldKey"
                          class="form-input"
                          [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                          [required]="field.isRequired"
                          #dfRef="ngModel"
                        />
                        @if (dfRef.invalid && (dfRef.dirty || dfRef.touched)) {
                          <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">{{ field.fieldName }} is required.</span>
                        }
                      }

                      <!-- Boolean type -->
                      @else if (field.fieldType === 'Boolean') {
                        <div class="flex align-center gap-8 mt-8">
                          <input
                            type="checkbox"
                            id="create-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                          />
                          <label for="create-df-{{field.fieldKey}}" style="font-size: 0.9rem; color: var(--text-main); font-weight: 500;">{{ field.fieldName }}</label>
                        </div>
                      }

                      <!-- Select type -->
                      @else if (field.fieldType === 'Select') {
                        <select
                          id="create-df-{{field.fieldKey}}"
                          [name]="'df_' + field.fieldKey"
                          class="form-select"
                          [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                          [required]="field.isRequired"
                          #dfRef="ngModel"
                        >
                          <option value="">-- Select --</option>
                          @for (opt of field.options; track opt) {
                            <option [value]="opt">{{ opt }}</option>
                          }
                        </select>
                        @if (dfRef.invalid && (dfRef.dirty || dfRef.touched)) {
                          <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">{{ field.fieldName }} is required.</span>
                        }
                      }

                      <!-- MultiSelect type -->
                      @else if (field.fieldType === 'MultiSelect') {
                        <div class="custom-multiselect-container">
                          <div class="multiselect-options flex flex-wrap gap-8 p-8 border rounded-8 bg-white mb-8" style="border: 1px solid var(--border); border-radius: 8px; min-height: 42px; display: flex; align-items: center; padding: 6px 12px; gap: 8px;">
                            @if (!newTaskDynamicValues[field.fieldKey] || newTaskDynamicValues[field.fieldKey].length === 0) {
                              <span class="text-muted" style="font-size: 0.85rem;">None selected</span>
                            } @else {
                              @for (selected of newTaskDynamicValues[field.fieldKey]; track selected) {
                                <span class="badge badge-status-todo flex align-center gap-4 animate-scale-up" style="font-size: 0.75rem; padding: 2px 8px; background-color: #e0e7ff; color: #4f46e5; border-radius: 9999px; display: inline-flex; align-items: center; gap: 4px;">
                                  {{ selected }}
                                  <span class="material-symbols-rounded cursor-pointer" style="font-size: 14px; font-weight: bold;" (click)="toggleMultiSelectOption(field.fieldKey, selected)">close</span>
                                </span>
                              }
                            }
                          </div>
                          
                          <select
                            multiple
                            id="create-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            class="form-select"
                            style="height: 100px;"
                            [(ngModel)]="newTaskDynamicValues[field.fieldKey]"
                            [required]="field.isRequired"
                            #dfRef="ngModel"
                            (change)="onMultiSelectChange($event, field.fieldKey)"
                          >
                            @for (opt of field.options; track opt) {
                              <option [value]="opt">{{ opt }}</option>
                            }
                          </select>
                        </div>
                        @if (dfRef.invalid && (dfRef.dirty || dfRef.touched)) {
                          <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">{{ field.fieldName }} is required.</span>
                        }
                      }
                    </div>
                  }
                }
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeCreateTaskModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="taskForm.invalid || creatingTask()">
                  {{ creatingTask() ? 'Creating...' : 'Create Task' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Add Member Modal -->
      @if (showAddMemberModal()) {
        <div class="modal-overlay" (click)="closeAddMemberModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Add Project Member</h3>
              <button class="close-btn" (click)="closeAddMemberModal()">&times;</button>
            </div>
            
            <form #memberForm="ngForm" (ngSubmit)="onAddMemberSubmit(memberForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="member-email">Enter User Email</label>
                  <input 
                    type="email" 
                    id="member-email" 
                    name="email" 
                    class="form-input" 
                    [(ngModel)]="newMemberData.email" 
                    #memEmail="ngModel" 
                    required
                    email
                    placeholder="e.g. user@example.com"
                  />
                  @if (memEmail.invalid && (memEmail.dirty || memEmail.touched)) {
                    <span class="error-text">Must be a valid email address.</span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="member-role">Project Role</label>
                  <select class="form-select" id="member-role" name="roleInProject" [(ngModel)]="newMemberData.roleInProject">
                    <option value="ProjectManager">Project Manager</option>
                    <option value="Member">Member</option>
                  </select>
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeAddMemberModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="memberForm.invalid || addingMember()">
                  {{ addingMember() ? 'Adding...' : 'Add Member' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Create/Edit Dynamic Field Modal -->
      @if (showDynamicFieldModal()) {
        <div class="modal-overlay" (click)="closeDynamicFieldModal()">
          <div class="modal-container animate-scale-up" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ editingField() ? 'Edit Dynamic Field' : 'Add Dynamic Field' }}</h3>
              <button class="close-btn" (click)="closeDynamicFieldModal()">&times;</button>
            </div>
            
            <form #dfForm="ngForm" (ngSubmit)="saveDynamicField(dfForm)">
              <div class="modal-body">
                <div class="form-group">
                  <label class="form-label" for="df-name">Field Name</label>
                  <input 
                    type="text" 
                    id="df-name" 
                    name="fieldName" 
                    class="form-input" 
                    [(ngModel)]="dynamicFieldData.fieldName" 
                    required 
                    maxlength="100"
                    placeholder="e.g. Release Version"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="df-key">Field Key</label>
                  <input 
                    type="text" 
                    id="df-key" 
                    name="fieldKey" 
                    class="form-input" 
                    [(ngModel)]="dynamicFieldData.fieldKey" 
                    required 
                    pattern="^[a-zA-Z][a-zA-Z0-9_]*$"
                    [disabled]="editingField() !== null"
                    placeholder="e.g. releaseVersion"
                    #keyInput="ngModel"
                  />
                  @if (keyInput.invalid && (keyInput.dirty || keyInput.touched)) {
                    <span class="error-text" style="color: var(--danger); font-size: 0.8rem; margin-top: 4px; display: block;">
                      Must start with a letter and contain only letters, numbers, and underscores.
                    </span>
                  }
                </div>

                <div class="form-group">
                  <label class="form-label" for="df-type">Field Type</label>
                  <select 
                    id="df-type" 
                    name="fieldType" 
                    class="form-select" 
                    [(ngModel)]="dynamicFieldData.fieldType"
                    [disabled]="editingField() !== null"
                  >
                    <option value="Text">Text</option>
                    <option value="Number">Number</option>
                    <option value="Date">Date</option>
                    <option value="Boolean">Boolean</option>
                    <option value="Select">Select</option>
                    <option value="MultiSelect">MultiSelect</option>
                  </select>
                </div>

                <div class="form-group">
                  <div class="flex align-center gap-8 mt-8">
                    <input 
                      type="checkbox" 
                      id="df-required" 
                      name="isRequired" 
                      [(ngModel)]="dynamicFieldData.isRequired"
                    />
                    <label for="df-required" style="font-size: 0.9rem; font-weight: 500;">Required (Mandatory)</label>
                  </div>
                </div>

                @if (dynamicFieldData.fieldType === 'Select' || dynamicFieldData.fieldType === 'MultiSelect') {
                  <div class="form-group">
                    <label class="form-label" for="df-options">Options (comma or newline separated)</label>
                    <textarea 
                      id="df-options" 
                      name="optionsRaw" 
                      class="form-input form-textarea" 
                      [(ngModel)]="dynamicFieldData.optionsRaw" 
                      required
                      placeholder="Option 1, Option 2, Option 3"
                    ></textarea>
                  </div>
                }

                <div class="form-group">
                  <label class="form-label" for="df-default">Default Value (Optional)</label>
                  <input 
                    type="text" 
                    id="df-default" 
                    name="defaultValue" 
                    class="form-input" 
                    [(ngModel)]="dynamicFieldData.defaultValue" 
                    placeholder="e.g. v1.0 or true or 123"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label" for="df-order">Display Order</label>
                  <input 
                    type="number" 
                    id="df-order" 
                    name="displayOrder" 
                    class="form-input" 
                    [(ngModel)]="dynamicFieldData.displayOrder" 
                    required
                  />
                </div>
              </div>
              
              <div class="modal-footer">
                <button type="button" class="btn btn-outline" (click)="closeDynamicFieldModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="dfForm.invalid || savingField()">
                  {{ savingField() ? 'Saving...' : 'Save Field' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .detail-container {
      width: 100%;
    }
    .project-name {
      font-size: 1.5rem;
      font-weight: 700;
      margin-bottom: 6px;
    }
    .project-desc {
      color: var(--text-muted);
      font-size: 0.9rem;
    }

    /* Tabs navigation */
    .tabs-container {
      display: flex;
      gap: 8px;
      border-bottom: 1px solid var(--border);
      padding-bottom: 2px;
    }
    .tab-btn {
      padding: 10px 18px;
      font-size: 0.9rem;
      font-weight: 600;
      color: var(--text-muted);
      background: none;
      border: none;
      border-bottom: 2px solid transparent;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 8px;
      transition: all var(--transition-fast);
    }
    .tab-btn span {
      font-size: 20px;
    }
    .tab-btn:hover {
      color: var(--text-main);
    }
    .tab-btn.active {
      color: var(--primary);
      border-bottom-color: var(--primary);
    }

    /* Tasks Kanban Board */
    .search-input {
      min-width: 250px;
    }
    .kanban-board {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 16px;
      overflow-x: auto;
      align-items: start;
    }
    @media (max-width: 1200px) {
      .kanban-board {
        grid-template-columns: repeat(3, 1fr);
      }
    }
    @media (max-width: 768px) {
      .kanban-board {
        grid-template-columns: 1fr;
      }
    }
    .kanban-col {
      background-color: #f1f5f9;
      border-radius: 12px;
      padding: 12px;
      display: flex;
      flex-direction: column;
      max-height: 70vh;
    }
    .kanban-col-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      padding: 0 4px;
    }
    .kanban-col-header h4 {
      font-size: 0.9rem;
      font-weight: 700;
    }
    .task-count {
      font-size: 0.75rem;
      font-weight: 600;
      background-color: #cbd5e1;
      color: #475569;
      padding: 2px 6px;
      border-radius: 6px;
    }
    .kanban-col-cards {
      display: flex;
      flex-direction: column;
      gap: 10px;
      overflow-y: auto;
      flex-grow: 1;
      min-height: 250px;
    }
    .kanban-card {
      background-color: #ffffff;
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px;
      cursor: grab;
      box-shadow: var(--shadow-sm);
      transition: all var(--transition-fast);
    }
    .kanban-card:hover {
      border-color: var(--primary);
      box-shadow: var(--shadow-md);
      transform: translateY(-1px);
    }
    .kanban-card.dragging {
      opacity: 0.4;
      border: 2px dashed var(--primary);
      box-shadow: none;
      transform: scale(0.96) rotate(1deg);
      cursor: grabbing;
    }
    .kanban-col-cards.drag-over {
      background-color: rgba(79, 70, 229, 0.04);
      border: 2px dashed rgba(79, 70, 229, 0.2);
      border-radius: 8px;
      padding: 10px;
    }
    .drag-placeholder {
      border: 2px dashed rgba(79, 70, 229, 0.4);
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
      background-color: rgba(79, 70, 229, 0.02);
      color: rgba(79, 70, 229, 0.6);
      font-size: 0.8rem;
      font-weight: 600;
      text-align: center;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 70px;
      pointer-events: none;
      animation: pulse-border 1.5s infinite ease-in-out;
    }
    @keyframes pulse-border {
      0% { border-color: rgba(79, 70, 229, 0.3); background-color: rgba(79, 70, 229, 0.01); }
      50% { border-color: rgba(79, 70, 229, 0.6); background-color: rgba(79, 70, 229, 0.04); }
      100% { border-color: rgba(79, 70, 229, 0.3); background-color: rgba(79, 70, 229, 0.01); }
    }
    .task-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--text-main);
    }
    .task-desc {
      font-size: 0.775rem;
      color: var(--text-muted);
      line-height: 1.4;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .card-footer {
      border-top: 1px solid rgba(226, 232, 240, 0.5);
    }
    .assignee-name {
      font-size: 0.75rem;
      color: var(--text-muted);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 60px;
    }
    .due-date {
      font-size: 0.7rem;
      color: var(--text-light);
    }
    .due-date.overdue {
      color: var(--danger);
      font-weight: 600;
    }

    /* Members Styling */
    .inline-select {
      max-width: 160px;
      padding: 4px 8px;
      font-size: 0.85rem;
    }
    .owner-indicator {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--primary);
    }
    .text-danger-color {
      color: var(--danger);
    }
    .text-danger-color:hover {
      background-color: var(--danger-bg);
    }

    /* Audit history styling */
    .audit-history {
      display: flex;
      flex-direction: column;
      gap: 16px;
      max-height: 500px;
      overflow-y: auto;
    }
    .audit-row {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding-bottom: 16px;
      border-bottom: 1px solid var(--border);
    }
    .audit-row:last-child {
      border-bottom: none;
      padding-bottom: 0;
    }
    .log-type-icon {
      font-size: 20px;
      color: var(--primary);
      padding: 6px;
      background-color: var(--primary-light);
      border-radius: 8px;
    }
    .log-details {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .log-title {
      font-size: 0.9rem;
      color: var(--text-main);
    }
    .log-time {
      font-size: 0.75rem;
      color: var(--text-light);
    }
    .log-diff {
      margin-top: 6px;
      padding: 8px 12px;
      background-color: #f8fafc;
      border-radius: 6px;
      font-family: monospace;
      font-size: 0.75rem;
      color: var(--text-muted);
      border: 1px solid var(--border);
    }
    .diff-old {
      color: #ef4444;
    }
    .diff-new {
      color: #10b981;
      margin-top: 2px;
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
    .active-toggle {
      background-color: #ffffff !important;
      color: var(--primary) !important;
      box-shadow: var(--shadow-sm);
    }
    .toggle-group button {
      border: none;
      background: none;
      cursor: pointer;
      color: var(--text-muted);
      transition: all var(--transition-fast);
    }
    .toggle-group button:hover {
      color: var(--text-main);
    }
    .clickable-row {
      cursor: pointer;
      transition: background-color var(--transition-fast);
    }
    .clickable-row:hover {
      background-color: #f8fafc;
    }
  `]
})
export class ProjectDetail implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);
  private readonly auditLogService = inject(AuditLogService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dynamicFieldService = inject(DynamicFieldService);

  protected project = signal<any | null>(null);
  protected members = signal<any[]>([]);
  protected tasks = signal<any[]>([]);
  protected auditLogs = signal<any[]>([]);
  protected dynamicFields = signal<any[]>([]);

  // Dynamic Fields management modal state
  protected showDynamicFieldModal = signal(false);
  protected editingField = signal<any | null>(null);
  protected dynamicFieldData = {
    fieldName: '',
    fieldKey: '',
    fieldType: 'Text',
    isRequired: false,
    optionsRaw: '',
    defaultValue: '',
    displayOrder: 0,
    isActive: true
  };
  protected savingField = signal(false);

  // New task dynamic values state
  protected newTaskDynamicValues: Record<string, any> = {};
  
  protected loading = signal(false);
  protected activeTab = signal('tasks');
  protected viewMode = signal<'kanban' | 'table'>('kanban');
  protected editOnOpen = signal(false);
  protected projectId: string = '';
  protected pendingCreateTask = false;

  // Task Kanban configuration
  protected kanbanColumns = [
    { status: 'Todo', name: 'Todo' },
    { status: 'InProgress', name: 'In Progress' },
    { status: 'InReview', name: 'In Review' },
    { status: 'Done', name: 'Completed' },
    { status: 'Cancelled', name: 'Cancelled' }
  ];

  // Filters
  protected taskFilters = {
    search: '',
    status: '',
    priority: '',
    page: 1,
    pageSize: 100 // Load all in board view
  };

  // Modals state
  protected showEditModal = signal(false);
  protected updatingProject = signal(false);
  protected editProjectData = { name: '', description: '', status: '' };

  protected showConfirmDelete = signal(false);

  protected showCreateTaskModal = signal(false);
  protected creatingTask = signal(false);
  protected newTaskData = { title: '', description: '', priority: 'Medium', assigneeId: null as string | null, dueDate: null as string | null, parentTaskId: null as string | null };

  protected showAddMemberModal = signal(false);
  protected addingMember = signal(false);
  protected newMemberData = { email: '', roleInProject: 'Member' };

  // Drag and Drop State
  protected draggedTask = signal<any | null>(null);
  protected activeDragOverStatus = signal<string | null>(null);

  // Task details drawer
  protected selectedTaskId = signal<string | null>(null);

  // Gantt Chart State
  protected timelineDays = signal<Date[]>([]);

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id') || '';
    if (this.projectId) {
      this.loadProjectDetails();
    }
    this.route.queryParams.subscribe(params => {
      if (params['createTask'] === 'true') {
        if (this.project()) {
          if (this.canCreateTask()) {
            this.openCreateTaskModal();
          } else {
            this.toastService.error('You do not have permission to create tasks in this project.');
          }
        } else {
          this.pendingCreateTask = true;
        }
        // Clear query parameters
        this.router.navigate([], { relativeTo: this.route, queryParams: { createTask: null }, queryParamsHandling: 'merge' });
      }
    });
  }

  loadProjectDetails() {
    this.loading.set(true);
    forkJoin({
      project: this.projectService.getProject(this.projectId),
      members: this.projectService.getMembers(this.projectId)
    }).subscribe({
      next: ({ project, members }) => {
        this.project.set(project);
        this.members.set(members || []);
        this.loadTasks();
        this.loadProjectDynamicFields();
        if (this.canViewAuditLogs()) {
          this.loadAuditLogs();
        }

        if (this.pendingCreateTask) {
          this.pendingCreateTask = false;
          if (this.canCreateTask()) {
            this.openCreateTaskModal();
          } else {
            this.toastService.error('You do not have permission to create tasks in this project.');
          }
        }
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load project details.');
        this.router.navigate(['/projects']);
      }
    });
  }

  loadTasks() {
    this.taskService.getProjectTasks(this.projectId, this.taskFilters).subscribe({
      next: (res) => {
        this.tasks.set(res.items || []);
        this.generateTimeline();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load tasks list.');
      }
    });
  }

  loadMembers() {
    this.projectService.getMembers(this.projectId).subscribe({
      next: (mList) => {
        this.members.set(mList || []);
      },
      error: () => {
        this.toastService.error('Failed to load project members.');
      }
    });
  }

  loadAuditLogs() {
    this.auditLogService.getProjectAuditLogs(this.projectId).subscribe({
      next: (logs) => {
        this.auditLogs.set(logs || []);
      },
      error: () => {
        // Soft fail
      }
    });
  }

  setTab(tab: string) {
    this.activeTab.set(tab);
    if (tab === 'audit') {
      this.loadAuditLogs();
    } else if (tab === 'members') {
      this.loadMembers();
    } else if (tab === 'dynamic-fields') {
      this.loadProjectDynamicFields();
    } else {
      this.loadTasks();
    }
  }

  loadProjectDynamicFields() {
    this.dynamicFieldService.getProjectDynamicFields(this.projectId).subscribe({
      next: (fields) => {
        this.dynamicFields.set(fields || []);
      },
      error: () => {
        this.toastService.error('Failed to load project dynamic fields.');
      }
    });
  }

  canManageDynamicFields(): boolean {
    const user = this.authService.currentUser();
    if (!user || !this.project()) return false;
    if (user.roles?.includes('Admin')) return true;
    
    const selfMember = this.members().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager';
  }

  openAddDynamicFieldModal() {
    this.editingField.set(null);
    this.dynamicFieldData = {
      fieldName: '',
      fieldKey: '',
      fieldType: 'Text',
      isRequired: false,
      optionsRaw: '',
      defaultValue: '',
      displayOrder: this.dynamicFields().length + 1,
      isActive: true
    };
    this.showDynamicFieldModal.set(true);
  }

  openEditDynamicFieldModal(field: any) {
    this.editingField.set(field);
    this.dynamicFieldData = {
      fieldName: field.fieldName,
      fieldKey: field.fieldKey,
      fieldType: field.fieldType,
      isRequired: field.isRequired,
      optionsRaw: field.options ? field.options.join(', ') : '',
      defaultValue: field.defaultValue || '',
      displayOrder: field.displayOrder,
      isActive: field.isActive
    };
    this.showDynamicFieldModal.set(true);
  }

  closeDynamicFieldModal() {
    this.showDynamicFieldModal.set(false);
    this.editingField.set(null);
  }

  saveDynamicField(form: any) {
    if (form.invalid) return;
    this.savingField.set(true);

    let options: string[] | null = null;
    if (this.dynamicFieldData.fieldType === 'Select' || this.dynamicFieldData.fieldType === 'MultiSelect') {
      options = this.dynamicFieldData.optionsRaw
        .split(/[,\n]/)
        .map(o => o.trim())
        .filter(o => o.length > 0);
    }

    const payload = {
      fieldName: this.dynamicFieldData.fieldName,
      fieldKey: this.dynamicFieldData.fieldKey,
      fieldType: this.dynamicFieldData.fieldType,
      isRequired: this.dynamicFieldData.isRequired,
      options: options,
      defaultValue: this.dynamicFieldData.defaultValue || null,
      displayOrder: this.dynamicFieldData.displayOrder,
      isActive: this.dynamicFieldData.isActive
    };

    const editField = this.editingField();
    if (editField) {
      this.dynamicFieldService.updateDynamicField(editField.id, payload).subscribe({
        next: () => {
          this.savingField.set(false);
          this.showDynamicFieldModal.set(false);
          this.toastService.success('Dynamic field updated.');
          this.loadProjectDynamicFields();
          this.loadAuditLogs();
        },
        error: (err) => {
          this.savingField.set(false);
          this.toastService.error(err.error?.message || 'Failed to update dynamic field.');
        }
      });
    } else {
      this.dynamicFieldService.createDynamicField(this.projectId, payload).subscribe({
        next: () => {
          this.savingField.set(false);
          this.showDynamicFieldModal.set(false);
          this.toastService.success('Dynamic field created.');
          this.loadProjectDynamicFields();
          this.loadAuditLogs();
        },
        error: (err) => {
          this.savingField.set(false);
          this.toastService.error(err.error?.message || 'Failed to create dynamic field.');
        }
      });
    }
  }

  deleteDynamicField(fieldId: string) {
    if (confirm('Are you sure you want to delete this dynamic field? This will delete all its values associated with tasks!')) {
      this.dynamicFieldService.deleteDynamicField(fieldId).subscribe({
        next: () => {
          this.toastService.success('Dynamic field deleted.');
          this.loadProjectDynamicFields();
          this.loadAuditLogs();
        },
        error: (err) => {
          this.toastService.error(err.error?.message || 'Failed to delete dynamic field.');
        }
      });
    }
  }

  toggleMultiSelectOption(fieldKey: string, option: string) {
    if (!this.newTaskDynamicValues[fieldKey]) {
      this.newTaskDynamicValues[fieldKey] = [];
    }
    const idx = this.newTaskDynamicValues[fieldKey].indexOf(option);
    if (idx >= 0) {
      this.newTaskDynamicValues[fieldKey].splice(idx, 1);
    } else {
      this.newTaskDynamicValues[fieldKey].push(option);
    }
  }

  onMultiSelectChange(event: any, fieldKey: string) {
    const selectedOptions = Array.from(event.target.selectedOptions).map((o: any) => o.value);
    this.newTaskDynamicValues[fieldKey] = selectedOptions;
  }

  // Permission Checks
  canEditProject(): boolean {
    const user = this.authService.currentUser();
    if (!user || !this.project()) return false;
    if (user.roles?.includes('Admin')) return true;
    if (this.project().ownerId === user.id) return true;
    
    const selfMember = this.members().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager';
  }

  canDeleteProject(): boolean {
    const user = this.authService.currentUser();
    return user?.roles?.includes('Admin') ?? false;
  }

  canCreateTask(): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    if (user.roles?.includes('Admin')) return true;
    if (this.project()?.ownerId === user.id) return true;

    const selfMember = this.members().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager' || selfMember?.roleInProject === 'Member';
  }

  canManageMembers(): boolean {
    return this.canEditProject();
  }

  canViewAuditLogs(): boolean {
    return this.canEditProject();
  }

  // Project Actions
  openEditModal() {
    if (!this.project()) return;
    this.editProjectData = {
      name: this.project().name,
      description: this.project().description,
      status: this.project().status
    };
    this.showEditModal.set(true);
  }

  closeEditModal() {
    this.showEditModal.set(false);
  }

  onEditSubmit(form: any) {
    if (form.invalid) return;
    this.updatingProject.set(true);
    this.projectService.updateProject(this.projectId, this.editProjectData).subscribe({
      next: (updated) => {
        this.project.set(updated);
        this.updatingProject.set(false);
        this.showEditModal.set(false);
        this.toastService.success('Project details updated.');
        this.loadAuditLogs();
      },
      error: (err) => {
        this.updatingProject.set(false);
        const msg = err.error?.message || 'Failed to update project.';
        this.toastService.error(msg);
      }
    });
  }

  triggerDeleteProject() {
    this.showConfirmDelete.set(true);
  }

  onDeleteConfirm() {
    this.showConfirmDelete.set(false);
    this.loading.set(true);
    this.projectService.deleteProject(this.projectId).subscribe({
      next: () => {
        this.toastService.success('Project deleted successfully.');
        this.router.navigate(['/projects']);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to delete project.');
      }
    });
  }

  onDeleteCancel() {
    this.showConfirmDelete.set(false);
  }

  // Tasks Methods
  onTaskFilterChange() {
    this.loadTasks();
  }

  getTasksByStatus(status: string): any[] {
    return this.tasks().filter(t => t.status === status);
  }

  getTaskCountInCol(status: string): number {
    return this.getTasksByStatus(status).length;
  }

  isOverdue(dueDateStr: string): boolean {
    const dueDate = new Date(dueDateStr);
    dueDate.setHours(23, 59, 59, 999);
    return dueDate.getTime() < Date.now();
  }

  getStatusLabel(status: string): string {
    if (status === 'InProgress') return 'In Progress';
    if (status === 'InReview') return 'In Review';
    return status;
  }

  openCreateTaskModal() {
    this.newTaskData = { title: '', description: '', priority: 'Medium', assigneeId: null, dueDate: null, parentTaskId: null };
    this.newTaskDynamicValues = {};
    this.dynamicFields().forEach(f => {
      if (f.isActive) {
        if (f.fieldType === 'Boolean') {
          this.newTaskDynamicValues[f.fieldKey] = f.defaultValue === 'true' || f.defaultValue === '1';
        } else if (f.fieldType === 'MultiSelect') {
          try {
            this.newTaskDynamicValues[f.fieldKey] = f.defaultValue ? JSON.parse(f.defaultValue) : [];
          } catch {
            this.newTaskDynamicValues[f.fieldKey] = [];
          }
        } else {
          this.newTaskDynamicValues[f.fieldKey] = f.defaultValue || '';
        }
      }
    });
    this.showCreateTaskModal.set(true);
  }

  closeCreateTaskModal() {
    this.showCreateTaskModal.set(false);
  }

  onCreateTaskSubmit(form: any) {
    if (form.invalid) return;
    this.creatingTask.set(true);

    const payloadDynamicValues: Record<string, string> = {};
    this.dynamicFields().forEach(field => {
      if (field.isActive) {
        const rawVal = this.newTaskDynamicValues[field.fieldKey];
        if (rawVal !== undefined && rawVal !== null && rawVal !== '') {
          if (field.fieldType === 'MultiSelect') {
            payloadDynamicValues[field.fieldKey] = Array.isArray(rawVal) ? JSON.stringify(rawVal) : JSON.stringify([rawVal]);
          } else if (field.fieldType === 'Boolean') {
            payloadDynamicValues[field.fieldKey] = rawVal ? 'true' : 'false';
          } else {
            payloadDynamicValues[field.fieldKey] = String(rawVal);
          }
        }
      }
    });

    const requestData = {
      ...this.newTaskData,
      dynamicValues: payloadDynamicValues
    };

    this.taskService.createTask(this.projectId, requestData).subscribe({
      next: () => {
        this.creatingTask.set(false);
        this.showCreateTaskModal.set(false);
        this.toastService.success('Task created successfully.');
        this.loadTasks();
        this.loadAuditLogs();
      },
      error: (err) => {
        this.creatingTask.set(false);
        const msg = err.error?.message || 'Failed to create task.';
        this.toastService.error(msg);
      }
    });
  }

  openTaskDetails(taskId: string) {
    this.editOnOpen.set(false);
    this.selectedTaskId.set(taskId);
  }

  openTaskDetailsAndEdit(taskId: string) {
    this.editOnOpen.set(true);
    this.selectedTaskId.set(taskId);
  }

  closeTaskDetails() {
    this.selectedTaskId.set(null);
    this.editOnOpen.set(false);
  }

  onTaskUpdated() {
    this.loadTasks();
    this.loadAuditLogs();
  }

  // Drag and Drop Handlers
  onDragStart(event: DragEvent, task: any) {
    this.draggedTask.set(task);
    // Add custom class to the target after a tiny timeout to keep the visual drag image intact
    setTimeout(() => {
      const card = event.target as HTMLElement;
      if (card) {
        card.classList.add('dragging');
      }
    }, 0);
  }

  onDragEnd(event: DragEvent) {
    this.draggedTask.set(null);
    this.activeDragOverStatus.set(null);
    const card = event.target as HTMLElement;
    if (card) {
      card.classList.remove('dragging');
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  onDragEnter(event: DragEvent, status: string) {
    event.preventDefault();
    this.activeDragOverStatus.set(status);
  }

  onDragLeave(event: DragEvent) {
    // Prevent flickering when dragging over child elements
    const relatedTarget = event.relatedTarget as HTMLElement;
    const currentTarget = event.currentTarget as HTMLElement;
    if (relatedTarget && currentTarget && currentTarget.contains(relatedTarget)) {
      return;
    }
    this.activeDragOverStatus.set(null);
  }

  onDrop(event: DragEvent, targetStatus: string) {
    event.preventDefault();
    const task = this.draggedTask();
    if (!task) return;

    const oldStatus = task.status;
    if (oldStatus === targetStatus) return;

    // Optimistic UI Update: immediately change status locally for fluid feel
    const updatedTasks = this.tasks().map(t => t.id === task.id ? { ...t, status: targetStatus } : t);
    this.tasks.set(updatedTasks);

    // Call service to update on backend
    this.taskService.updateTaskStatus(task.id, targetStatus).subscribe({
      next: () => {
        this.toastService.success(`Task status updated to ${this.getStatusLabel(targetStatus)}`);
        this.loadTasks();
        this.loadAuditLogs();
      },
      error: (err) => {
        // Revert optimistic update
        const revertedTasks = this.tasks().map(t => t.id === task.id ? { ...t, status: oldStatus } : t);
        this.tasks.set(revertedTasks);
        
        const msg = err.error?.message || 'Failed to update task status.';
        this.toastService.error(msg);
      }
    });

    this.draggedTask.set(null);
    this.activeDragOverStatus.set(null);
  }

  // Members Management
  openAddMemberModal() {
    this.newMemberData = { email: '', roleInProject: 'Member' };
    this.showAddMemberModal.set(true);
  }

  closeAddMemberModal() {
    this.showAddMemberModal.set(false);
  }

  onAddMemberSubmit(form: any) {
    if (form.invalid) return;
    this.addingMember.set(true);
    this.projectService.addMember(this.projectId, this.newMemberData).subscribe({
      next: () => {
        this.addingMember.set(false);
        this.showAddMemberModal.set(false);
        this.toastService.success('Member added successfully.');
        this.loadMembers();
        this.loadAuditLogs();
      },
      error: (err) => {
        this.addingMember.set(false);
        const msg = err.error?.message || 'Failed to add project member.';
        this.toastService.error(msg);
      }
    });
  }

  changeMemberRole(userId: string, event: any) {
    const role = event.target.value;
    this.projectService.updateMemberRole(this.projectId, userId, role).subscribe({
      next: () => {
        this.toastService.success('Member role updated.');
        this.loadMembers();
        this.loadAuditLogs();
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to update member role.';
        this.toastService.error(msg);
        this.loadMembers(); // Revert UI
      }
    });
  }

  removeMember(userId: string) {
    if (confirm('Are you sure you want to remove this member from the project?')) {
      this.projectService.removeMember(this.projectId, userId).subscribe({
        next: () => {
          this.toastService.success('Member removed from project.');
          this.loadMembers();
          this.loadAuditLogs();
        },
        error: (err) => {
          const msg = err.error?.message || 'Failed to remove member.';
          this.toastService.error(msg);
        }
      });
    }
  }

  // Audit Logs Helpers
  getAuditIcon(type: string): string {
    switch (type) {
      case 'Project': return 'folder';
      case 'ProjectMember': return 'group';
      case 'Task': return 'assignment';
      case 'TaskComment': return 'comment';
      case 'TaskAttachment': return 'attach_file';
      default: return 'history';
    }
  }

  getAuditActionLabel(action: string): string {
    if (action.endsWith('Created') || action.endsWith('Added')) return 'created';
    if (action.endsWith('Updated') || action.endsWith('Modified') || action.endsWith('UpdatedRole')) return 'updated';
    if (action.endsWith('Deleted') || action.endsWith('Removed')) return 'deleted';
    if (action.endsWith('StatusChanged')) return 'changed status of';
    if (action.endsWith('AssigneeChanged')) return 'reassigned';
    return action.toLowerCase();
  }

  // Gantt Chart Calculations
  generateTimeline() {
    const projectTasks = this.tasks();
    if (projectTasks.length === 0) {
      const start = new Date();
      start.setDate(start.getDate() - 5);
      const end = new Date();
      end.setDate(end.getDate() + 25);
      this.timelineDays.set(this.getDaysArray(start, end));
      return;
    }

    let minDate: Date | null = null;
    let maxDate: Date | null = null;

    projectTasks.forEach(task => {
      const created = new Date(task.createdAt);
      if (minDate === null || created < minDate) {
        minDate = created;
      }
      
      const due = task.dueDate ? new Date(task.dueDate) : created;
      if (maxDate === null || due > maxDate) {
        maxDate = due;
      }
    });

    const finalMin = minDate ? new Date(minDate) : new Date();
    const finalMax = maxDate ? new Date(maxDate) : new Date();

    // Clamp timeline to avoid extreme spans
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
    if (finalMin < thirtyDaysAgo) {
      finalMin.setTime(thirtyDaysAgo.getTime());
    }

    const ninetyDaysAhead = new Date();
    ninetyDaysAhead.setDate(ninetyDaysAhead.getDate() + 90);
    if (finalMax > ninetyDaysAhead) {
      finalMax.setTime(ninetyDaysAhead.getTime());
    }

    // Ensure min range of 30 days
    const minTimelineEnd = new Date(finalMin);
    minTimelineEnd.setDate(minTimelineEnd.getDate() + 30);
    if (finalMax < minTimelineEnd) {
      finalMax.setTime(minTimelineEnd.getTime());
    }

    finalMin.setHours(0, 0, 0, 0);
    finalMax.setHours(0, 0, 0, 0);

    this.timelineDays.set(this.getDaysArray(finalMin, finalMax));
  }

  getDaysArray(start: Date, end: Date): Date[] {
    const days: Date[] = [];
    const current = new Date(start);
    while (current <= end) {
      days.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }
    return days;
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear();
  }

  getTaskBarPosition(task: any): string | null {
    const days = this.timelineDays();
    if (days.length === 0) return null;

    const start = new Date(task.createdAt);
    start.setHours(0, 0, 0, 0);

    const end = task.dueDate ? new Date(task.dueDate) : new Date(start);
    end.setHours(0, 0, 0, 0);

    const timelineStart = days[0];
    const timelineEnd = days[days.length - 1];

    if (end < timelineStart || start > timelineEnd) {
      return null;
    }

    let startIndex = days.findIndex(d => d.getTime() === start.getTime());
    if (startIndex === -1) {
      startIndex = 0;
    }

    let endIndex = days.findIndex(d => d.getTime() === end.getTime());
    if (endIndex === -1) {
      endIndex = days.length - 1;
    }

    return `${startIndex + 1} / ${endIndex + 2}`;
  }
}

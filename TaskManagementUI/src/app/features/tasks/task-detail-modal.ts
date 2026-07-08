import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { CommentService } from '../../core/services/comment.service';
import { AttachmentService } from '../../core/services/attachment.service';
import { AuditLogService } from '../../core/services/audit-log.service';
import { ProjectService } from '../../core/services/project.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { SafeHtmlPipe } from '../../shared/pipes/safe-html.pipe';
import { DynamicFieldService } from '../../core/services/dynamic-field.service';

@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, Avatar, LoadingSpinner, SafeHtmlPipe],
  template: `
    <div class="drawer-overlay" (click)="onClose()">
      <div class="drawer-container animate-slide-in" (click)="$event.stopPropagation()">
        <app-loading-spinner [active]="loading()"></app-loading-spinner>

        <!-- Drawer Header -->
        <div class="drawer-header flex justify-between align-center">
          <div class="header-details flex align-center gap-12">
            <span class="material-symbols-rounded">assignment</span>
            <h3>Task Details</h3>
          </div>
          <button class="close-btn" (click)="onClose()">&times;</button>
        </div>

        <!-- Drawer Content -->
        @if (task()) {
          <div class="drawer-body">
            <!-- Edit Form vs View Info -->
            <div class="glass-card mb-24">
              @if (!isEditing()) {
                <div class="view-task-info">
                  <div class="flex justify-between align-center mb-16">
                    <span class="badge" [ngClass]="'badge-status-' + task().status.toLowerCase()">
                      {{ getStatusLabel(task().status) }}
                    </span>
                    <span class="badge" [ngClass]="'badge-priority-' + task().priority.toLowerCase()">
                      {{ task().priority }}
                    </span>
                  </div>
                  <h2 class="mb-12">{{ task().title }}</h2>
                  
                  <div class="desc-text mb-20" [innerHTML]="task().description || 'No description provided.' | safeHtml"></div>

                  <div class="task-details-grid">
                    <div class="detail-item">
                      <span class="detail-label">Assignee</span>
                      <div class="detail-value flex align-center gap-8">
                        <app-avatar [name]="task().assigneeName || 'Unassigned'" [size]="28"></app-avatar>
                        <span>{{ task().assigneeName || 'Unassigned' }}</span>
                      </div>
                    </div>
                    <div class="detail-item">
                      <span class="detail-label">Due Date</span>
                      <div class="detail-value text-muted">
                        <span class="material-symbols-rounded">calendar_month</span>
                        {{ task().dueDate ? (task().dueDate | date:'mediumDate') : 'No due date' }}
                      </div>
                    </div>
                    <div class="detail-item">
                      <span class="detail-label">Created By</span>
                      <div class="detail-value text-muted">
                        {{ task().createdByName }} ({{ task().createdAt | date:'shortDate' }})
                      </div>
                    </div>
                    <div class="detail-item">
                      <span class="detail-label">Parent Task</span>
                      <div class="detail-value">
                        @if (task().parentTaskId) {
                          <a href="javascript:void(0)" (click)="navigateToTask(task().parentTaskId)" class="flex align-center gap-4 text-primary font-semibold" style="font-size: 0.85rem; text-decoration: none;">
                            <span class="material-symbols-rounded" style="font-size: 16px;">subdirectory_arrow_right</span>
                            {{ task().parentTaskTitle }}
                          </a>
                        } @else {
                          <span class="text-muted" style="font-size: 0.85rem;">-</span>
                        }
                      </div>
                    </div>

                    <!-- Dynamic Field Values -->
                    @for (field of dynamicFields(); track field.id) {
                      @if (field.isActive) {
                        <div class="detail-item">
                          <span class="detail-label">{{ field.fieldName }}</span>
                          <span class="detail-value" style="font-weight: 500; color: var(--text-main);">
                            @if (field.fieldType === 'Boolean') {
                              <span class="badge" [style.background-color]="task().dynamicValues && task().dynamicValues[field.fieldKey] === 'true' ? '#dcfce7' : '#f1f5f9'" [style.color]="task().dynamicValues && task().dynamicValues[field.fieldKey] === 'true' ? '#15803d' : '#475569'">
                                {{ task().dynamicValues && task().dynamicValues[field.fieldKey] === 'true' ? 'Yes' : 'No' }}
                              </span>
                            } @else if (field.fieldType === 'MultiSelect') {
                              @if (task().dynamicValues && task().dynamicValues[field.fieldKey]) {
                                <div class="flex flex-wrap gap-4" style="display: flex; flex-wrap: wrap; gap: 4px; margin-top: 4px;">
                                  @for (val of parseJsonArray(task().dynamicValues[field.fieldKey]); track val) {
                                    <span class="badge badge-sm" style="background-color: #e0e7ff; color: #4f46e5; font-size: 0.75rem; padding: 2px 6px;">{{ val }}</span>
                                  }
                                </div>
                              } @else {
                                <span class="text-muted" style="font-size: 0.85rem;">-</span>
                              }
                            } @else {
                              {{ task().dynamicValues && task().dynamicValues[field.fieldKey] ? task().dynamicValues[field.fieldKey] : '-' }}
                            }
                          </span>
                        </div>
                      }
                    }

                    @if (task().childTasks && task().childTasks.length > 0) {
                      <div class="detail-item col-span-2 mt-12" style="grid-column: span 2;">
                        <span class="detail-label mb-8 block">Subtasks</span>
                        <div class="subtasks-list flex flex-col gap-8">
                          @for (sub of task().childTasks; track sub.id) {
                            <div class="subtask-item flex align-center justify-between p-8 bg-gray-50 border rounded-8 clickable-row" (click)="navigateToTask(sub.id)" style="padding: 8px 12px; border: 1px solid var(--border); border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: space-between; background-color: #f8fafc; transition: all var(--transition-fast);">
                              <span class="subtask-title flex align-center gap-8 font-medium text-sm" style="display: inline-flex; align-items: center; gap: 8px; font-size: 0.85rem; font-weight: 500;">
                                <span class="material-symbols-rounded text-muted" style="font-size: 16px;">subdirectory_arrow_right</span>
                                {{ sub.title }}
                              </span>
                              <span class="badge badge-sm" [ngClass]="'badge-status-' + sub.status.toLowerCase()">
                                {{ getStatusLabel(sub.status) }}
                              </span>
                            </div>
                          }
                        </div>
                      </div>
                    }
                  </div>

                  @if (canEditOwnTask()) {
                    <button class="btn btn-outline mt-16 w-full" (click)="startEdit()">
                      <span class="material-symbols-rounded">edit</span>
                      Edit Task Details
                    </button>
                  }
                </div>
              } @else {
                <!-- Edit Mode Form -->
                <form #editForm="ngForm" (ngSubmit)="onSave(editForm)" class="edit-task-form">
                  <div class="form-group">
                    <label class="form-label" for="edit-title">Title</label>
                    <input 
                      type="text" 
                      id="edit-title" 
                      name="title" 
                      class="form-input" 
                      [(ngModel)]="editData.title" 
                      required 
                      maxlength="200"
                    />
                  </div>

                  <div class="form-group">
                    <div class="flex justify-between align-center mb-4" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">
                      <label class="form-label" for="edit-desc" style="margin-bottom: 0;">Description</label>
                      <div class="flex gap-8 align-center" style="display: flex; gap: 8px; align-items: center;">
                        @if (!isDescPreview()) {
                          <div class="flex gap-4" style="display: flex; gap: 4px;">
                            <button type="button" class="btn btn-sm btn-text p-4" (click)="insertTag('<strong>', '</strong>')" title="Bold" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded" style="font-size: 16px;">format_bold</span>
                            </button>
                            <button type="button" class="btn btn-sm btn-text p-4" (click)="insertTag('<em>', '</em>')" title="Italic" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded" style="font-size: 16px;">format_italic</span>
                            </button>
                            <button type="button" class="btn btn-sm btn-text p-4" (click)="insertLink()" title="Insert Link" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded" style="font-size: 16px;">link</span>
                            </button>
                            <button type="button" class="btn btn-sm btn-text p-4" (click)="insertTag('<ul>\n  <li>', '</li>\n</ul>')" title="Bullet List" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded" style="font-size: 16px;">format_list_bulleted</span>
                            </button>
                          </div>
                        }
                        <button type="button" class="btn btn-sm btn-outline py-2 px-8" style="font-size: 0.75rem; padding: 2px 8px;" (click)="isDescPreview.set(!isDescPreview())">
                          {{ isDescPreview() ? 'Edit' : 'Preview' }}
                        </button>
                      </div>
                    </div>

                    @if (!isDescPreview()) {
                      <textarea 
                        id="edit-desc" 
                        name="description" 
                        class="form-input form-textarea" 
                        [(ngModel)]="editData.description" 
                        maxlength="5000"
                      ></textarea>
                    } @else {
                      <div class="form-input form-textarea overflow-y-auto" style="min-height: 100px; background-color: #f8fafc; border: 1px solid var(--border); border-radius: 6px; padding: 8px 12px;" [innerHTML]="editData.description | safeHtml"></div>
                    }
                  </div>

                  <div class="grid grid-cols-2 gap-16">
                    <div class="form-group">
                      <label class="form-label" for="edit-status">Status</label>
                      <select class="form-select" id="edit-status" name="status" [(ngModel)]="editData.status">
                        <option value="Todo">Todo</option>
                        <option value="InProgress">In Progress</option>
                        <option value="InReview">In Review</option>
                        <option value="Done">Done</option>
                        <option value="Cancelled">Cancelled</option>
                      </select>
                    </div>

                    <div class="form-group">
                      <label class="form-label" for="edit-priority">Priority</label>
                      <select class="form-select" id="edit-priority" name="priority" [(ngModel)]="editData.priority">
                        <option value="Low">Low</option>
                        <option value="Medium">Medium</option>
                        <option value="High">High</option>
                        <option value="Critical">Critical</option>
                      </select>
                    </div>
                  </div>

                  <div class="grid grid-cols-2 gap-16">
                    <div class="form-group">
                      <label class="form-label" for="edit-assignee">Assignee</label>
                      <select class="form-select" id="edit-assignee" name="assigneeId" [(ngModel)]="editData.assigneeId">
                        <option [value]="null">Unassigned</option>
                        @for (m of projectMembers(); track m.userId) {
                          <option [value]="m.userId">{{ m.fullName }}</option>
                        }
                      </select>
                    </div>

                    <div class="form-group">
                      <label class="form-label" for="edit-due">Due Date</label>
                      <input 
                        type="date" 
                        id="edit-due" 
                        name="dueDate" 
                        class="form-input" 
                        [ngModel]="editData.dueDate | date:'yyyy-MM-dd'"
                        (ngModelChange)="editData.dueDate = $event"
                      />
                    </div>
                  </div>

                  <div class="grid grid-cols-1 mt-12">
                    <div class="form-group">
                      <label class="form-label" for="edit-parent">Parent Task</label>
                      <select class="form-select" id="edit-parent" name="parentTaskId" [(ngModel)]="editData.parentTaskId">
                        <option [value]="null">No Parent Task</option>
                        @for (t of projectTasks(); track t.id) {
                          @if (t.id !== taskId) {
                            <option [value]="t.id">{{ t.title }}</option>
                          }
                        }
                      </select>
                    </div>
                  </div>

                  <!-- Dynamic Fields -->
                  @for (field of dynamicFields(); track field.id) {
                    @if (field.isActive) {
                      <div class="form-group mt-12">
                        <label class="form-label" for="edit-df-{{field.fieldKey}}">
                          {{ field.fieldName }}
                          @if (field.isRequired) {
                            <span style="color: var(--danger);">*</span>
                          }
                        </label>

                        <!-- Text type -->
                        @if (field.fieldType === 'Text') {
                          <input
                            type="text"
                            id="edit-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            class="form-input"
                            [(ngModel)]="editDynamicValues[field.fieldKey]"
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
                            id="edit-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            class="form-input"
                            [(ngModel)]="editDynamicValues[field.fieldKey]"
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
                            id="edit-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            class="form-input"
                            [(ngModel)]="editDynamicValues[field.fieldKey]"
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
                              id="edit-df-{{field.fieldKey}}"
                              [name]="'df_' + field.fieldKey"
                              [(ngModel)]="editDynamicValues[field.fieldKey]"
                            />
                            <label for="edit-df-{{field.fieldKey}}" style="font-size: 0.9rem; color: var(--text-main); font-weight: 500;">{{ field.fieldName }}</label>
                          </div>
                        }

                        <!-- Select type -->
                        @else if (field.fieldType === 'Select') {
                          <select
                            id="edit-df-{{field.fieldKey}}"
                            [name]="'df_' + field.fieldKey"
                            class="form-select"
                            [(ngModel)]="editDynamicValues[field.fieldKey]"
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
                              @if (!editDynamicValues[field.fieldKey] || editDynamicValues[field.fieldKey].length === 0) {
                                <span class="text-muted" style="font-size: 0.85rem;">None selected</span>
                              } @else {
                                @for (selected of editDynamicValues[field.fieldKey]; track selected) {
                                  <span class="badge badge-status-todo flex align-center gap-4 animate-scale-up" style="font-size: 0.75rem; padding: 2px 8px; background-color: #e0e7ff; color: #4f46e5; border-radius: 9999px; display: inline-flex; align-items: center; gap: 4px;">
                                    {{ selected }}
                                    <span class="material-symbols-rounded cursor-pointer" style="font-size: 14px; font-weight: bold;" (click)="toggleEditMultiSelectOption(field.fieldKey, selected)">close</span>
                                  </span>
                                }
                              }
                            </div>
                            
                            <select
                              multiple
                              id="edit-df-{{field.fieldKey}}"
                              [name]="'df_' + field.fieldKey"
                              class="form-select"
                              style="height: 100px;"
                              [(ngModel)]="editDynamicValues[field.fieldKey]"
                              [required]="field.isRequired"
                              #dfRef="ngModel"
                              (change)="onEditMultiSelectChange($event, field.fieldKey)"
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

                  <div class="edit-actions flex gap-12 mt-16">
                    <button type="button" class="btn btn-outline flex-grow" (click)="cancelEdit()">Cancel</button>
                    <button type="submit" class="btn btn-primary flex-grow" [disabled]="editForm.invalid || saving()">
                      {{ saving() ? 'Saving...' : 'Save' }}
                    </button>
                  </div>
                </form>
              }
            </div>

            <!-- Tabs selector for Task Sub-sections -->
            <div class="sub-tabs flex gap-16 mb-16 border-b">
              <button class="sub-tab-btn" [ngClass]="{ 'active': activeSubTab() === 'comments' }" (click)="activeSubTab.set('comments')">
                Comments ({{ comments().length }})
              </button>
              <button class="sub-tab-btn" [ngClass]="{ 'active': activeSubTab() === 'files' }" (click)="activeSubTab.set('files')">
                Attachments ({{ attachments().length }})
              </button>
              <button class="sub-tab-btn" [ngClass]="{ 'active': activeSubTab() === 'audit' }" (click)="activeSubTab.set('audit')">
                Activity
              </button>
            </div>

            <!-- COMMENTS SUB-TAB -->
            @if (activeSubTab() === 'comments') {
              <div class="comments-section">
                <!-- Add Comment Form -->
                <form #commentForm="ngForm" (ngSubmit)="onAddComment(commentForm)" class="comment-input-form mb-16">
                  <div class="flex gap-12 align-center">
                    <input 
                      type="text" 
                      name="content" 
                      class="form-input" 
                      placeholder="Write a comment..." 
                      [(ngModel)]="newCommentText" 
                      required
                      maxlength="2000"
                    />
                    <button type="submit" class="btn btn-primary" [disabled]="commentForm.invalid || postingComment()">
                      Post
                    </button>
                  </div>
                </form>

                <!-- Comments Thread -->
                <div class="comments-list">
                  @if (comments().length === 0) {
                    <div class="text-center py-16 text-muted font-small">No comments posted yet.</div>
                  } @else {
                    @for (cmt of comments(); track cmt.id) {
                      <div class="comment-card animate-fade-in-up">
                        <div class="comment-header flex justify-between align-center mb-4">
                          <div class="comment-author flex align-center gap-8">
                            <app-avatar [name]="cmt.userFullName" [avatarUrl]="cmt.userAvatarUrl" [size]="24"></app-avatar>
                            <strong>{{ cmt.userFullName }}</strong>
                            <span class="comment-date text-light">{{ cmt.createdAt | date:'short' }}</span>
                          </div>
                          @if (canDeleteComment(cmt)) {
                            <button class="btn btn-text delete-cmt-btn" (click)="onDeleteComment(cmt.id)">
                              &times;
                            </button>
                          }
                        </div>
                        <div class="comment-body">{{ cmt.content }}</div>
                      </div>
                    }
                  }
                </div>
              </div>
            }

            <!-- ATTACHMENTS SUB-TAB -->
            @if (activeSubTab() === 'files') {
              <div class="files-section">
                <!-- File Selector Dropzone -->
                <div class="file-uploader-box mb-16">
                  <label class="uploader-label flex flex-col align-center justify-center gap-8">
                    <span class="material-symbols-rounded upload-icon">cloud_upload</span>
                    <span class="upload-title">Click to upload image</span>
                    <span class="upload-desc">Max 20MB. Image types only.</span>
                    <input 
                      type="file" 
                      class="hidden-file-input" 
                      (change)="onFileSelected($event)" 
                      accept="image/*"
                      [disabled]="uploadingFile()"
                    />
                  </label>
                  @if (uploadingFile()) {
                    <div class="uploader-overlay flex align-center justify-center">
                      <span>Uploading...</span>
                    </div>
                  }
                </div>

                <!-- Attachments Grid -->
                <div class="attachments-grid">
                  @if (attachments().length === 0) {
                    <div class="text-center py-16 text-muted font-small">No files attached yet.</div>
                  } @else {
                    <div class="grid grid-cols-2 gap-16">
                      @for (file of attachments(); track file.id) {
                        <div class="file-card flex align-center justify-between gap-12">
                          <div class="file-info flex align-center gap-8 min-width-0" (click)="file.objectUrl ? openLightbox(file.objectUrl) : null" style="cursor: pointer;">
                            @if (file.objectUrl) {
                              <img [src]="file.objectUrl" style="width: 40px; height: 40px; object-fit: cover; border-radius: 4px; border: 1px solid var(--border);" />
                            } @else {
                              <span class="material-symbols-rounded file-icon">image</span>
                            }
                            <div class="file-name-meta min-width-0">
                              <div class="file-name" [title]="file.fileName" style="font-weight: 600;">{{ file.fileName }}</div>
                              <div class="file-size text-light">{{ formatBytes(file.fileSize) }}</div>
                            </div>
                          </div>
                          <div class="file-actions flex gap-8">
                            <button class="btn btn-text text-muted" (click)="downloadFile(file); $event.stopPropagation()" title="Download" style="padding: 4px; min-width: auto;">
                              <span class="material-symbols-rounded">download</span>
                            </button>
                            @if (canDeleteAttachment(file)) {
                              <button class="btn btn-text text-danger-color" (click)="deleteFile(file.id); $event.stopPropagation()" title="Delete" style="padding: 4px; min-width: auto;">
                                <span class="material-symbols-rounded">delete</span>
                              </button>
                            }
                          </div>
                        </div>
                      }
                    </div>
                  }
                </div>
              </div>
            }

            <!-- AUDIT ACTIVITY SUB-TAB -->
            @if (activeSubTab() === 'audit') {
              <div class="task-audit-logs">
                @if (auditLogs().length === 0) {
                  <div class="text-center py-16 text-muted">No activity logs recorded.</div>
                } @else {
                  <div class="audit-history">
                    @for (log of auditLogs(); track log.id) {
                      <div class="audit-row animate-fade-in-up">
                        <div class="log-details">
                          <div class="log-title">
                            <strong>{{ log.changedByName }}</strong>
                            {{ log.friendlyDescription }}
                          </div>
                          <div class="log-time">{{ log.changedAt | date:'short' }}</div>
                          @if (log.oldValueFormatted || log.newValueFormatted) {
                            <div class="log-diff">
                              @if (log.oldValueFormatted) {
                                <div class="diff-old">Before: {{ log.oldValueFormatted }}</div>
                              }
                              @if (log.newValueFormatted) {
                                <div class="diff-new">After: {{ log.newValueFormatted }}</div>
                              }
                            </div>
                          }
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
      
      <!-- Lightbox Image Preview Modal -->
      @if (lightboxUrl()) {
        <div class="modal-overlay" (click)="closeLightbox()" style="z-index: 1200; background-color: rgba(15, 23, 42, 0.75); display: flex; align-items: center; justify-content: center; position: fixed; top: 0; left: 0; right: 0; bottom: 0;">
          <div class="modal-container" (click)="$event.stopPropagation()" style="max-width: 90vw; max-height: 90vh; background: none; border: none; padding: 0; box-shadow: none; display: flex; flex-direction: column; align-items: center; justify-content: center; position: relative;">
            <button class="close-btn" (click)="closeLightbox()" style="position: absolute; top: -40px; right: 0; color: #ffffff; font-size: 2.5rem; background: none; border: none; cursor: pointer;">&times;</button>
            <img [src]="lightboxUrl()" style="max-width: 100%; max-height: 80vh; border-radius: 8px; box-shadow: var(--shadow-lg);" />
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .drawer-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(15, 23, 42, 0.3);
      backdrop-filter: blur(4px);
      z-index: 1100;
      display: flex;
      justify-content: flex-end;
    }
    .drawer-container {
      width: 100%;
      max-width: 520px;
      height: 100%;
      background-color: #ffffff;
      border-left: 1px solid var(--border);
      box-shadow: -10px 0 30px rgba(15, 23, 42, 0.08);
      display: flex;
      flex-direction: column;
      animation: slideIn var(--transition-normal) forwards;
      position: relative;
    }
    .drawer-header {
      padding: 20px 24px;
      border-bottom: 1px solid var(--border);
      flex-shrink: 0;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.75rem;
      cursor: pointer;
      color: var(--text-light);
      line-height: 1;
    }
    .close-btn:hover {
      color: var(--text-muted);
    }
    .drawer-body {
      flex-grow: 1;
      padding: 24px;
      overflow-y: auto;
    }

    .desc-text {
      color: var(--text-muted);
      font-size: 0.95rem;
      line-height: 1.5;
      white-space: pre-wrap;
    }
    .desc-text a, .form-textarea a {
      color: var(--primary);
      text-decoration: underline;
      font-weight: 500;
      transition: color var(--transition-fast);
    }
    .desc-text a:hover, .form-textarea a:hover {
      color: #4338ca;
    }

    /* Details Grid */
    .task-details-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 16px;
    }
    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .detail-label {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--text-light);
      text-transform: uppercase;
    }
    .detail-value {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-main);
    }

    /* Sub Tabs selector */
    .sub-tabs {
      border-bottom: 1px solid var(--border);
    }
    .sub-tab-btn {
      background: none;
      border: none;
      padding: 8px 4px;
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-muted);
      cursor: pointer;
      border-bottom: 2px solid transparent;
      transition: all var(--transition-fast);
    }
    .sub-tab-btn:hover {
      color: var(--text-main);
    }
    .sub-tab-btn.active {
      color: var(--primary);
      border-bottom-color: var(--primary);
    }

    /* Comments Section */
    .comments-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
      max-height: 300px;
      overflow-y: auto;
    }
    .comment-card {
      background-color: #f8fafc;
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px;
    }
    .comment-author {
      font-size: 0.85rem;
    }
    .comment-date {
      font-size: 0.7rem;
    }
    .comment-body {
      font-size: 0.875rem;
      color: var(--text-main);
      margin-top: 4px;
      white-space: pre-wrap;
    }
    .delete-cmt-btn {
      font-size: 1.25rem;
      color: var(--text-light);
      padding: 0 4px;
      cursor: pointer;
    }
    .delete-cmt-btn:hover {
      color: var(--danger);
      background: none;
    }

    /* File upload box */
    .file-uploader-box {
      border: 2px dashed var(--border);
      border-radius: 12px;
      background-color: #f8fafc;
      position: relative;
      overflow: hidden;
      transition: border-color var(--transition-fast);
    }
    .file-uploader-box:hover {
      border-color: var(--primary);
    }
    .uploader-label {
      padding: 24px;
      cursor: pointer;
      width: 100%;
    }
    .upload-icon {
      font-size: 32px;
      color: var(--primary);
    }
    .upload-title {
      font-size: 0.875rem;
      font-weight: 600;
    }
    .upload-desc {
      font-size: 0.75rem;
      color: var(--text-light);
    }
    .hidden-file-input {
      display: none;
    }
    .uploader-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(255,255,255,0.85);
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-main);
    }

    /* Attachments */
    .attachments-grid {
      max-height: 250px;
      overflow-y: auto;
    }
    .file-card {
      padding: 10px 14px;
      background-color: #ffffff;
      border: 1px solid var(--border);
      border-radius: 8px;
    }
    .file-icon {
      color: var(--primary);
      font-size: 24px;
    }
    .file-name {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-main);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .file-size {
      font-size: 0.7rem;
    }
    .text-danger-color {
      color: var(--danger);
    }
    .text-danger-color:hover {
      background-color: var(--danger-bg);
    }
    .min-width-0 {
      min-width: 0;
    }

    /* Audit Logs styling */
    .task-audit-logs {
      max-height: 300px;
      overflow-y: auto;
    }
    .audit-history {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .audit-row {
      border-bottom: 1px solid var(--border);
      padding-bottom: 8px;
    }
    .audit-row:last-child {
      border-bottom: none;
    }
    .log-details {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .log-title {
      font-size: 0.85rem;
      color: var(--text-main);
    }
    .log-time {
      font-size: 0.7rem;
      color: var(--text-light);
    }
    .log-diff {
      margin-top: 4px;
      padding: 6px;
      font-size: 0.7rem;
      border-radius: 4px;
    }

    /* Keyframes slide in from right */
    @keyframes slideIn {
      from {
        transform: translateX(100%);
      }
      to {
        transform: translateX(0);
      }
    }
  `]
})
export class TaskDetailModal implements OnInit, OnDestroy {
  @Input() taskId: string = '';
  @Input() startInEditMode = false;
  @Output() close = new EventEmitter<void>();
  @Output() taskUpdated = new EventEmitter<void>();

  private readonly taskService = inject(TaskService);
  private readonly commentService = inject(CommentService);
  private readonly attachmentService = inject(AttachmentService);
  private readonly auditLogService = inject(AuditLogService);
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly dynamicFieldService = inject(DynamicFieldService);

  protected task = signal<any | null>(null);
  protected comments = signal<any[]>([]);
  protected attachments = signal<any[]>([]);
  protected auditLogs = signal<any[]>([]);
  protected projectMembers = signal<any[]>([]);
  protected projectTasks = signal<any[]>([]);
  protected dynamicFields = signal<any[]>([]);
  protected editDynamicValues: Record<string, any> = {};

  protected loading = signal(false);
  protected activeSubTab = signal('comments');

  // Edit Mode state
  protected isEditing = signal(false);
  protected saving = signal(false);
  protected editData = { title: '', description: '', status: '', priority: '', assigneeId: null as string | null, dueDate: null as string | null, parentTaskId: null as string | null };
  protected isDescPreview = signal(false);

  // Lightbox preview state
  protected lightboxUrl = signal<string | null>(null);

  // Comments state
  protected newCommentText: string = '';
  protected postingComment = signal(false);

  // File upload state
  protected uploadingFile = signal(false);

  ngOnInit(): void {
    if (this.startInEditMode) {
      this.isEditing.set(true);
    }
    if (this.taskId) {
      this.loadTaskDetails();
    }
  }

  loadTaskDetails() {
    this.loading.set(true);
    this.taskService.getTask(this.taskId).subscribe({
      next: (t) => {
        this.task.set(t);
        this.editData = {
          title: t.title,
          description: t.description,
          status: t.status,
          priority: t.priority,
          assigneeId: t.assigneeId || null,
          dueDate: t.dueDate,
          parentTaskId: t.parentTaskId || null
        };

        this.loadProjectDynamicFields(t.projectId, t.dynamicValues || {});
        
        // Fetch project members list for assignee dropdown in editing
        this.projectService.getMembers(t.projectId).subscribe({
          next: (mList) => {
            this.projectMembers.set(mList || []);
          }
        });

        // Fetch project tasks list for parent task selection in editing
        this.taskService.getProjectTasks(t.projectId, { search: '', status: '', priority: '', page: 1, pageSize: 100 }).subscribe({
          next: (res) => {
            this.projectTasks.set(res.items || []);
          }
        });

        this.loadComments();
        this.loadAttachments();
        this.loadAuditLogs();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load task details.');
        this.onClose();
      }
    });
  }

  loadProjectDynamicFields(projectId: string, taskDynamicValues: any) {
    this.dynamicFieldService.getProjectDynamicFields(projectId).subscribe({
      next: (fields) => {
        this.dynamicFields.set(fields || []);
        
        this.editDynamicValues = {};
        fields.forEach((f: any) => {
          const rawVal = taskDynamicValues[f.fieldKey];
          if (f.fieldType === 'Boolean') {
            this.editDynamicValues[f.fieldKey] = rawVal === 'true' || rawVal === '1';
          } else if (f.fieldType === 'MultiSelect') {
            try {
              this.editDynamicValues[f.fieldKey] = rawVal ? JSON.parse(rawVal) : [];
            } catch {
              this.editDynamicValues[f.fieldKey] = [];
            }
          } else {
            this.editDynamicValues[f.fieldKey] = rawVal || '';
          }
        });
      }
    });
  }

  toggleEditMultiSelectOption(fieldKey: string, option: string) {
    if (!this.editDynamicValues[fieldKey]) {
      this.editDynamicValues[fieldKey] = [];
    }
    const idx = this.editDynamicValues[fieldKey].indexOf(option);
    if (idx >= 0) {
      this.editDynamicValues[fieldKey].splice(idx, 1);
    } else {
      this.editDynamicValues[fieldKey].push(option);
    }
  }

  onEditMultiSelectChange(event: any, fieldKey: string) {
    const selectedOptions = Array.from(event.target.selectedOptions).map((o: any) => o.value);
    this.editDynamicValues[fieldKey] = selectedOptions;
  }

  parseJsonArray(str: string | null | undefined): string[] {
    if (!str) return [];
    try {
      const parsed = JSON.parse(str);
      return Array.isArray(parsed) ? parsed : [parsed];
    } catch {
      return [str];
    }
  }

  loadComments() {
    this.commentService.getComments(this.taskId).subscribe({
      next: (cList) => {
        this.comments.set(cList || []);
      }
    });
  }

  loadAttachments() {
    this.attachmentService.getAttachments(this.taskId).subscribe({
      next: (aList) => {
        const listWithUrls = (aList || []).map((a: any) => ({ ...a, objectUrl: null }));
        this.attachments.set(listWithUrls);

        listWithUrls.forEach((a: any) => {
          this.attachmentService.downloadAttachment(a.id).subscribe({
            next: (blob) => {
              const url = window.URL.createObjectURL(blob);
              this.attachments.update(curr => curr.map(item => item.id === a.id ? { ...item, objectUrl: url } : item));
            }
          });
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.attachments().forEach(a => {
      if (a.objectUrl) {
        window.URL.revokeObjectURL(a.objectUrl);
      }
    });
  }

  loadAuditLogs() {
    this.auditLogService.getTaskAuditLogs(this.taskId).subscribe({
      next: (logs) => {
        const enriched = (logs || []).map((l: any) => this.enrichAuditLog(l));
        this.auditLogs.set(enriched);
      }
    });
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

  formatBytes(bytes: number, decimals = 2) {
    if (!+bytes) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`;
  }

  onClose() {
    this.close.emit();
  }

  // Edit Task
  startEdit() {
    this.isEditing.set(true);
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.isDescPreview.set(false);
    // Revert editData
    const t = this.task();
    this.editData = {
      title: t.title,
      description: t.description,
      status: t.status,
      priority: t.priority,
      assigneeId: t.assigneeId || null,
      dueDate: t.dueDate,
      parentTaskId: t.parentTaskId || null
    };

    // Revert dynamic values
    this.editDynamicValues = {};
    const taskValues = t.dynamicValues || {};
    this.dynamicFields().forEach(f => {
      const rawVal = taskValues[f.fieldKey];
      if (f.fieldType === 'Boolean') {
        this.editDynamicValues[f.fieldKey] = rawVal === 'true' || rawVal === '1';
      } else if (f.fieldType === 'MultiSelect') {
        try {
          this.editDynamicValues[f.fieldKey] = rawVal ? JSON.parse(rawVal) : [];
        } catch {
          this.editDynamicValues[f.fieldKey] = [];
        }
      } else {
        this.editDynamicValues[f.fieldKey] = rawVal || '';
      }
    });
  }

  onSave(form: any) {
    if (form.invalid) return;

    this.saving.set(true);

    const payloadDynamicValues: Record<string, string> = {};
    this.dynamicFields().forEach(field => {
      if (field.isActive) {
        const rawVal = this.editDynamicValues[field.fieldKey];
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

    const updatePayload = {
      title: this.editData.title,
      description: this.editData.description,
      status: this.editData.status,
      priority: this.editData.priority,
      assigneeId: this.editData.assigneeId === 'null' || !this.editData.assigneeId ? null : this.editData.assigneeId,
      dueDate: this.editData.dueDate || null,
      parentTaskId: this.editData.parentTaskId === 'null' || !this.editData.parentTaskId ? null : this.editData.parentTaskId,
      rowVersion: this.task().rowVersion, // Send original base64 rowVersion
      dynamicValues: payloadDynamicValues
    };

    this.taskService.updateTask(this.taskId, updatePayload).subscribe({
      next: (updated) => {
        this.task.set(updated);
        this.editData = {
          title: updated.title,
          description: updated.description,
          status: updated.status,
          priority: updated.priority,
          assigneeId: updated.assigneeId || null,
          dueDate: updated.dueDate,
          parentTaskId: updated.parentTaskId || null
        };
        this.isEditing.set(false);
        this.isDescPreview.set(false);
        this.saving.set(false);
        this.toastService.success('Task details saved.');
        this.loadTaskDetails(); // Full reload to get new titles & child lists
        this.taskUpdated.emit();
      },
      error: (err) => {
        this.saving.set(false);
        if (err.status === 409) {
          // Optimistic Concurrency Conflict captured!
          this.toastService.error('The task was modified by another user. Please reload the task and try again.');
          // Force reload task details to get new RowVersion
          this.loadTaskDetails();
        } else {
          const msg = err.error?.message || 'Failed to save changes.';
          this.toastService.error(msg);
        }
      }
    });
  }

  canEditOwnTask(): boolean {
    const user = this.authService.currentUser();
    if (!user || !this.task()) return false;
    if (user.roles?.includes('Admin')) return true;
    if (this.task().assigneeId === user.id) return true;

    // Check project member role (PM can edit any task)
    const selfMember = this.projectMembers().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager';
  }

  // Comments Management
  onAddComment(form: any) {
    if (form.invalid) return;

    this.postingComment.set(true);
    this.commentService.createComment(this.taskId, this.newCommentText).subscribe({
      next: () => {
        this.postingComment.set(false);
        this.newCommentText = '';
        this.toastService.success('Comment posted.');
        this.loadComments();
        this.loadAuditLogs();
      },
      error: () => {
        this.postingComment.set(false);
        this.toastService.error('Failed to post comment.');
      }
    });
  }

  canDeleteComment(cmt: any): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    if (user.roles?.includes('Admin')) return true;
    if (cmt.userId === user.id) return true;

    const selfMember = this.projectMembers().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager';
  }

  onDeleteComment(commentId: string) {
    if (confirm('Are you sure you want to delete this comment?')) {
      this.commentService.deleteComment(this.taskId, commentId).subscribe({
        next: () => {
          this.toastService.success('Comment deleted.');
          this.loadComments();
          this.loadAuditLogs();
        },
        error: () => {
          this.toastService.error('Failed to delete comment.');
        }
      });
    }
  }

  // File Upload & download
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    // Size limit 20MB check
    if (file.size > 20971520) {
      this.toastService.error('File size exceeds the limit of 20MB.');
      return;
    }

    // Type extension check
    const allowed = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
    const ext = '.' + file.name.split('.').pop().toLowerCase();
    if (!allowed.includes(ext)) {
      this.toastService.error('Only image files are allowed (.jpg, .jpeg, .png, .gif, .webp).');
      return;
    }

    this.uploadingFile.set(true);
    this.attachmentService.uploadAttachment(this.taskId, file).subscribe({
      next: () => {
        this.uploadingFile.set(false);
        this.toastService.success('File uploaded successfully.');
        this.loadAttachments();
        this.loadAuditLogs();
        this.taskUpdated.emit();
      },
      error: (err) => {
        this.uploadingFile.set(false);
        const msg = err.error?.message || 'Failed to upload file.';
        this.toastService.error(msg);
      }
    });
  }

  downloadFile(attachment: any) {
    this.attachmentService.downloadAttachment(attachment.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = attachment.fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.toastService.error('Failed to download file.');
      }
    });
  }

  canDeleteAttachment(file: any): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    if (user.roles?.includes('Admin')) return true;
    if (file.uploadedById === user.id) return true;

    const selfMember = this.projectMembers().find(m => m.userId === user.id);
    return selfMember?.roleInProject === 'ProjectManager';
  }

  deleteFile(fileId: string) {
    if (confirm('Are you sure you want to delete this file attachment?')) {
      this.attachmentService.deleteAttachment(fileId).subscribe({
        next: () => {
          this.toastService.success('Attachment deleted.');
          this.loadAttachments();
          this.loadAuditLogs();
          this.taskUpdated.emit();
        },
        error: () => {
          this.toastService.error('Failed to delete attachment.');
        }
      });
    }
  }

  // Audit Logs Helpers
  getAuditActionLabel(action: string): string {
    if (action.endsWith('Created')) return 'created';
    if (action.endsWith('Updated')) return 'modified';
    if (action.endsWith('Deleted')) return 'deleted';
    if (action.endsWith('StatusChanged')) return 'updated status of';
    if (action.endsWith('AssigneeChanged')) return 'reassigned';
    if (action.endsWith('CommentAdded')) return 'commented on';
    if (action.endsWith('CommentDeleted')) return 'removed comment from';
    if (action.endsWith('AttachmentUploaded')) return 'attached file to';
    if (action.endsWith('AttachmentDeleted')) return 'deleted file from';
    return action.toLowerCase();
  }

  insertTag(openTag: string, closeTag: string) {
    const textarea = document.getElementById('edit-desc') as HTMLTextAreaElement;
    if (!textarea) return;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const text = textarea.value;
    const selected = text.substring(start, end);
    const replacement = openTag + selected + closeTag;
    this.editData.description = text.substring(0, start) + replacement + text.substring(end);
    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + openTag.length, start + openTag.length + selected.length);
    }, 50);
  }

  insertLink() {
    const url = prompt('Enter the link URL (e.g. https://example.com):');
    if (url) {
      this.insertTag(`<a href="${url}" target="_blank" rel="noopener noreferrer">`, '</a>');
    }
  }

  navigateToTask(id: string) {
    this.taskId = id;
    this.isEditing.set(false);
    this.isDescPreview.set(false);
    this.loadTaskDetails();
  }

  openLightbox(url: string) {
    this.lightboxUrl.set(url);
  }

  closeLightbox() {
    this.lightboxUrl.set(null);
  }

  enrichAuditLog(log: any): any {
    let friendlyDescription = '';
    let oldValueFormatted = log.oldValue;
    let newValueFormatted = log.newValue;

    let entityName = '';
    if (log.entityType === 'Task') {
      entityName = this.task() ? `task "${this.task().title}"` : `task #${log.entityId.substring(0, 8)}`;
    } else if (log.entityType === 'TaskComment') {
      entityName = `comment`;
    } else if (log.entityType === 'TaskAttachment') {
      entityName = `attachment`;
    } else {
      entityName = `${log.entityType.toLowerCase()} #${log.entityId.substring(0, 8)}`;
    }

    switch (log.action) {
      case 'TaskCreated':
        friendlyDescription = `created ${entityName}`;
        oldValueFormatted = null;
        newValueFormatted = null;
        break;
      case 'TaskUpdated':
        friendlyDescription = `updated details of ${entityName}`;
        const diff = this.parseAndDiff(log.oldValue, log.newValue, this.projectMembers(), this.projectTasks());
        oldValueFormatted = diff.oldValueFormatted;
        newValueFormatted = diff.newValueFormatted;
        break;
      case 'TaskDeleted':
        friendlyDescription = `deleted ${entityName}`;
        break;
      case 'TaskStatusChanged':
        friendlyDescription = `changed status of ${entityName}`;
        if (log.oldValue) oldValueFormatted = this.formatStatus(log.oldValue);
        if (log.newValue) newValueFormatted = this.formatStatus(log.newValue);
        break;
      case 'TaskPriorityChanged':
        friendlyDescription = `changed priority of ${entityName}`;
        break;
      case 'TaskAssigneeChanged':
        friendlyDescription = `reassigned ${entityName}`;
        if (log.oldValue) {
          const m = this.projectMembers().find((x: any) => x.userId === log.oldValue);
          oldValueFormatted = m ? m.fullName : 'Unassigned';
        } else {
          oldValueFormatted = 'Unassigned';
        }
        if (log.newValue) {
          const m = this.projectMembers().find((x: any) => x.userId === log.newValue);
          newValueFormatted = m ? m.fullName : 'Unassigned';
        } else {
          newValueFormatted = 'Unassigned';
        }
        break;
      case 'CommentAdded':
        friendlyDescription = `added a ${entityName}`;
        break;
      case 'CommentDeleted':
        friendlyDescription = `removed a ${entityName}`;
        break;
      case 'AttachmentUploaded':
      case 'AttachmentAdded':
        friendlyDescription = `uploaded an ${entityName}`;
        break;
      case 'AttachmentDeleted':
        friendlyDescription = `removed an ${entityName}`;
        break;
      default:
        friendlyDescription = `${this.getAuditActionLabel(log.action)} ${entityName}`;
        break;
    }

    return {
      ...log,
      friendlyDescription,
      oldValueFormatted,
      newValueFormatted
    };
  }

  getUserName(userId: string, members: any[]): string {
    if (!userId) return 'Unassigned';
    const m = (members || []).find((x: any) => x.userId === userId || x.id === userId);
    return m ? m.fullName : 'Unassigned';
  }

  getTaskTitle(taskId: string, tasks: any[]): string {
    if (!taskId) return 'None';
    const t = (tasks || []).find((x: any) => x.id === taskId);
    return t ? t.title : 'None';
  }

  parseAndDiff(oldValStr: string, newValStr: string, projectMembers: any[], tasks: any[]): { oldValueFormatted: string | null, newValueFormatted: string | null } {
    if (!oldValStr || !newValStr) {
      return { oldValueFormatted: oldValStr, newValueFormatted: newValStr };
    }
    try {
      const isOldJson = oldValStr.trim().startsWith('{') && oldValStr.trim().endsWith('}');
      const isNewJson = newValStr.trim().startsWith('{') && newValStr.trim().endsWith('}');
      if (isOldJson && isNewJson) {
        const oldObj = JSON.parse(oldValStr);
        const newObj = JSON.parse(newValStr);
        
        const oldChanges: string[] = [];
        const newChanges: string[] = [];
        
        const keys = Array.from(new Set([...Object.keys(oldObj), ...Object.keys(newObj)]));
        for (const key of keys) {
          if (key === 'DynamicValues') {
            const oldDyn = oldObj[key] || {};
            const newDyn = newObj[key] || {};
            const dynKeys = Array.from(new Set([...Object.keys(oldDyn), ...Object.keys(newDyn)]));
            for (const dk of dynKeys) {
              const ov = oldDyn[dk];
              const nv = newDyn[dk];
              if (ov !== nv) {
                oldChanges.push(`${dk}: "${ov || 'None'}"`);
                newChanges.push(`${dk}: "${nv || 'None'}"`);
              }
            }
            continue;
          }
          
          const ov = oldObj[key];
          const nv = newObj[key];
          
          if (JSON.stringify(ov) !== JSON.stringify(nv)) {
            let label = key;
            let ovFormatted = ov;
            let nvFormatted = nv;
            
            if (key === 'AssigneeId') {
              label = 'Assignee';
              ovFormatted = this.getUserName(ov, projectMembers);
              nvFormatted = this.getUserName(nv, projectMembers);
            } else if (key === 'ParentTaskId') {
              label = 'Parent Task';
              ovFormatted = this.getTaskTitle(ov, tasks);
              nvFormatted = this.getTaskTitle(nv, tasks);
            } else if (key === 'DueDate') {
              label = 'Due Date';
              ovFormatted = ov ? new Date(ov).toLocaleDateString() : 'None';
              nvFormatted = nv ? new Date(nv).toLocaleDateString() : 'None';
            } else if (key === 'Status') {
              ovFormatted = this.formatStatus(ov);
              nvFormatted = this.formatStatus(nv);
            }
            
            const ovString = (ovFormatted !== null && ovFormatted !== undefined && ovFormatted !== '') ? `"${ovFormatted}"` : 'None';
            const nvString = (nvFormatted !== null && nvFormatted !== undefined && nvFormatted !== '') ? `"${nvFormatted}"` : 'None';
            
            oldChanges.push(`${label}: ${ovString}`);
            newChanges.push(`${label}: ${nvString}`);
          }
        }
        
        return {
          oldValueFormatted: oldChanges.length > 0 ? oldChanges.join(', ') : null,
          newValueFormatted: newChanges.length > 0 ? newChanges.join(', ') : null
        };
      }
    } catch (e) {
      // ignore
    }
    return { oldValueFormatted: oldValStr, newValueFormatted: newValStr };
  }

  formatStatus(status: string): string {
    if (status === 'InProgress') return 'In Progress';
    if (status === 'InReview') return 'In Review';
    return status;
  }
}

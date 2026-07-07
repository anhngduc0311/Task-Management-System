import { Component, OnInit, Input, Output, EventEmitter, inject, signal } from '@angular/core';
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

@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, Avatar, LoadingSpinner],
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
                  <p class="desc-text mb-20">{{ task().description || 'No description provided.' }}</p>

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
                    <label class="form-label" for="edit-desc">Description</label>
                    <textarea 
                      id="edit-desc" 
                      name="description" 
                      class="form-input form-textarea" 
                      [(ngModel)]="editData.description" 
                      maxlength="5000"
                    ></textarea>
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
                          <div class="file-info flex align-center gap-8 min-width-0">
                            <span class="material-symbols-rounded file-icon">image</span>
                            <div class="file-name-meta min-width-0">
                              <div class="file-name" [title]="file.fileName">{{ file.fileName }}</div>
                              <div class="file-size text-light">{{ formatBytes(file.fileSize) }}</div>
                            </div>
                          </div>
                          <div class="file-actions flex gap-8">
                            <button class="btn btn-text text-muted" (click)="downloadFile(file)" title="Download">
                              <span class="material-symbols-rounded">download</span>
                            </button>
                            @if (canDeleteAttachment(file)) {
                              <button class="btn btn-text text-danger-color" (click)="deleteFile(file.id)" title="Delete">
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
                            {{ getAuditActionLabel(log.action) }} task
                          </div>
                          <div class="log-time">{{ log.changedAt | date:'short' }}</div>
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
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
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
export class TaskDetailModal implements OnInit {
  @Input() taskId: string = '';
  @Output() close = new EventEmitter<void>();
  @Output() taskUpdated = new EventEmitter<void>();

  private readonly taskService = inject(TaskService);
  private readonly commentService = inject(CommentService);
  private readonly attachmentService = inject(AttachmentService);
  private readonly auditLogService = inject(AuditLogService);
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  protected task = signal<any | null>(null);
  protected comments = signal<any[]>([]);
  protected attachments = signal<any[]>([]);
  protected auditLogs = signal<any[]>([]);
  protected projectMembers = signal<any[]>([]);

  protected loading = signal(false);
  protected activeSubTab = signal('comments');

  // Edit Mode state
  protected isEditing = signal(false);
  protected saving = signal(false);
  protected editData = { title: '', description: '', status: '', priority: '', assigneeId: null as string | null, dueDate: null as string | null };

  // Comments state
  protected newCommentText: string = '';
  protected postingComment = signal(false);

  // File upload state
  protected uploadingFile = signal(false);

  ngOnInit(): void {
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
          dueDate: t.dueDate
        };
        
        // Fetch project members list for assignee dropdown in editing
        this.projectService.getMembers(t.projectId).subscribe({
          next: (mList) => {
            this.projectMembers.set(mList || []);
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
        this.attachments.set(aList || []);
      }
    });
  }

  loadAuditLogs() {
    this.auditLogService.getTaskAuditLogs(this.taskId).subscribe({
      next: (logs) => {
        this.auditLogs.set(logs || []);
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
    // Revert editData
    const t = this.task();
    this.editData = {
      title: t.title,
      description: t.description,
      status: t.status,
      priority: t.priority,
      assigneeId: t.assigneeId || null,
      dueDate: t.dueDate
    };
  }

  onSave(form: any) {
    if (form.invalid) return;

    this.saving.set(true);
    const updatePayload = {
      title: this.editData.title,
      description: this.editData.description,
      status: this.editData.status,
      priority: this.editData.priority,
      assigneeId: this.editData.assigneeId === 'null' || !this.editData.assigneeId ? null : this.editData.assigneeId,
      dueDate: this.editData.dueDate || null,
      rowVersion: this.task().rowVersion // Send original base64 rowVersion
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
          dueDate: updated.dueDate
        };
        this.isEditing.set(false);
        this.saving.set(false);
        this.toastService.success('Task details saved.');
        this.loadAuditLogs();
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
}

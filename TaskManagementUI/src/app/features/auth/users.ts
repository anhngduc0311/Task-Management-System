import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { ToastService } from '../../core/services/toast.service';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { Avatar } from '../../shared/components/avatar';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinner, Avatar],
  template: `
    <div class="users-container">
      <app-loading-spinner [active]="loading()"></app-loading-spinner>

      <!-- Upper Controls -->
      <div class="glass-card mb-24 flex justify-between align-center flex-wrap gap-16">
        <input 
          type="text" 
          class="form-input search-input" 
          placeholder="Search users..." 
          [(ngModel)]="searchQuery"
          (input)="onSearchChange()"
        />
      </div>

      <!-- Users Table -->
      <div class="glass-card">
        <div class="responsive-table-container">
          <table class="responsive-table">
            <thead>
              <tr>
                <th>User Details</th>
                <th class="hide-mobile">Email Address</th>
                <th>System Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (users().length === 0) {
                <tr>
                  <td colspan="4" class="text-center py-24 text-muted">
                    No users found.
                  </td>
                </tr>
              } @else {
                @for (u of users(); track u.id) {
                  <tr class="animate-fade-in-up">
                    <td>
                      <div class="flex align-center gap-12">
                        <app-avatar [name]="u.fullName" [avatarUrl]="u.avatarUrl" [size]="36"></app-avatar>
                        <div>
                          <strong>{{ u.fullName }}</strong>
                          <div class="mobile-only-meta show-mobile mt-4" style="display: none; font-size: 0.75rem; color: var(--text-muted);">
                            {{ u.email }}
                          </div>
                          <div class="user-id-text hide-mobile">ID: {{ u.id }}</div>
                        </div>
                      </div>
                    </td>
                    <td class="hide-mobile">{{ u.email }}</td>
                    <td>
                      <span class="badge" [ngClass]="u.status === 'Active' ? 'badge-status-done' : 'badge-status-cancelled'">
                        {{ u.status }}
                      </span>
                    </td>
                    <td>
                      <button 
                        class="btn" 
                        [ngClass]="u.status === 'Active' ? 'btn-danger' : 'btn-primary'"
                        (click)="toggleUserStatus(u)"
                      >
                        {{ u.status === 'Active' ? 'Deactivate' : 'Activate' }}
                      </button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Pagination -->
      @if (totalUsers() > pageSize) {
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
    </div>
  `,
  styles: [`
    .users-container {
      width: 100%;
    }
    .search-input {
      max-width: 320px;
    }
    .user-id-text {
      font-size: 0.7rem;
      color: var(--text-light);
      font-family: monospace;
      margin-top: 2px;
    }
    .text-center {
      text-align: center;
    }
    .py-24 {
      padding-top: 24px;
      padding-bottom: 24px;
    }
    .page-indicator {
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-muted);
    }
    @media (max-width: 768px) {
      .search-input {
        max-width: 100% !important;
        width: 100% !important;
      }
    }
  `]
})
export class Users implements OnInit {
  private readonly userService = inject(UserService);
  private readonly toastService = inject(ToastService);

  protected users = signal<any[]>([]);
  protected loading = signal(false);

  // Pagination & Filtering state
  protected currentPage = signal(1);
  protected pageSize = 10;
  protected totalUsers = signal(0);
  protected totalPages = signal(0);
  protected searchQuery = '';

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.userService.getUsers(this.currentPage(), this.pageSize, this.searchQuery).subscribe({
      next: (res) => {
        this.users.set(res.items || []);
        this.totalUsers.set(res.totalCount || 0);
        this.totalPages.set(Math.ceil(this.totalUsers() / this.pageSize));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.error('Failed to load users list.');
      }
    });
  }

  onSearchChange() {
    this.currentPage.set(1);
    this.loadUsers();
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadUsers();
  }

  toggleUserStatus(user: any) {
    const nextStatus = user.status === 'Active' ? 'Inactive' : 'Active';
    const actionText = user.status === 'Active' ? 'deactivate' : 'activate';

    if (confirm(`Are you sure you want to ${actionText} user ${user.fullName}?`)) {
      this.loading.set(true);
      this.userService.updateUserStatus(user.id, nextStatus).subscribe({
        next: () => {
          this.toastService.success(`User status updated to ${nextStatus}.`);
          this.loadUsers();
        },
        error: (err) => {
          this.loading.set(false);
          const msg = err.error?.message || 'Failed to update user status.';
          this.toastService.error(msg);
        }
      });
    }
  }
}

import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from './avatar';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, Avatar],
  template: `
    <div class="layout-container">
      <!-- Mobile Sidebar Overlay Backdrop -->
      @if (sidebarOpen()) {
        <div class="sidebar-overlay animate-fade-in" (click)="closeSidebar()"></div>
      }

      <!-- Sidebar -->
      <aside class="sidebar" [class.open]="sidebarOpen()">
        <div class="brand">
          <span class="material-symbols-rounded logo-icon">blur_on</span>
          <span class="brand-name">Taskly</span>
        </div>

        <nav class="nav-links">
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-item" (click)="closeSidebar()">
            <span class="material-symbols-rounded">dashboard</span>
            <span>Dashboard</span>
          </a>
          <a routerLink="/my-tasks" routerLinkActive="active" class="nav-item" (click)="closeSidebar()">
            <span class="material-symbols-rounded">task_alt</span>
            <span>My Tasks</span>
          </a>
          <a routerLink="/projects" routerLinkActive="active" class="nav-item" (click)="closeSidebar()">
            <span class="material-symbols-rounded">folder_open</span>
            <span>Projects</span>
          </a>
          <a routerLink="/reports" routerLinkActive="active" class="nav-item" (click)="closeSidebar()">
            <span class="material-symbols-rounded">bar_chart</span>
            <span>Reports</span>
          </a>
          @if (isAdmin()) {
            <a routerLink="/users" routerLinkActive="active" class="nav-item" (click)="closeSidebar()">
              <span class="material-symbols-rounded">group</span>
              <span>Users</span>
            </a>
          }
        </nav>

        <div class="sidebar-footer">
          <div class="user-profile">
            <app-avatar [name]="getUserName()" [avatarUrl]="getUserAvatar()" [size]="40"></app-avatar>
            <div class="user-info">
              <div class="user-name">{{ getUserName() }}</div>
              <div class="user-role">{{ isAdmin() ? 'System Admin' : 'Member' }}</div>
            </div>
          </div>
          <button class="logout-btn" (click)="onLogout()" title="Logout">
            <span class="material-symbols-rounded">logout</span>
          </button>
        </div>
      </aside>

      <!-- Main Panel -->
      <main class="main-panel">
        <header class="header">
          <div class="header-left" style="display: flex; align-items: center; gap: 12px;">
            <button class="hamburger-btn" (click)="toggleSidebar()" title="Toggle Menu">
              <span class="material-symbols-rounded">menu</span>
            </button>
            <h2 class="page-title">{{ getPageTitle() }}</h2>
          </div>
          <div class="header-right" style="display: flex; align-items: center; gap: 16px;">
            <div class="quick-actions" style="position: relative;">
              <button class="btn btn-primary" (click)="toggleQuickMenu()" style="padding: 6px 12px; font-size: 0.85rem; border-radius: 8px; display: inline-flex; align-items: center; gap: 4px;">
                <span class="material-symbols-rounded" style="font-size: 16px;">add</span>
                Quick Create
              </button>
              
              @if (showQuickMenu()) {
                <div class="glass-card" (click)="$event.stopPropagation()" style="position: absolute; top: 100%; right: 0; margin-top: 8px; z-index: 1000; min-width: 160px; padding: 8px; display: flex; flex-direction: column; gap: 4px; box-shadow: var(--shadow-md); background: white; border: 1px solid var(--border); border-radius: 8px;">
                  @if (canCreateProject()) {
                    <button class="btn btn-text" (click)="quickCreateProject()" style="padding: 8px; font-size: 0.85rem; text-align: left; display: flex; align-items: center; gap: 8px; width: 100%; background: none; border: none; cursor: pointer; color: var(--text-main);">
                      <span class="material-symbols-rounded" style="font-size: 18px; color: var(--primary);">folder</span>
                      New Project
                    </button>
                  }
                  <button class="btn btn-text" (click)="quickCreateTask()" style="padding: 8px; font-size: 0.85rem; text-align: left; display: flex; align-items: center; gap: 8px; width: 100%; background: none; border: none; cursor: pointer; color: var(--text-main);">
                    <span class="material-symbols-rounded" style="font-size: 18px; color: var(--primary);">assignment</span>
                    Create Task
                  </button>
                </div>
              }
            </div>

            <div class="welcome-text">
              Hello, <strong>{{ getUserName() }}</strong>
            </div>
          </div>
        </header>

        <!-- Content Area -->
        <div class="content-container">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .layout-container {
      display: flex;
      height: 100vh;
      width: 100vw;
      overflow: hidden;
      background-color: var(--bg-base);
    }
    
    /* Sidebar styling */
    .sidebar {
      width: 260px;
      background-color: var(--bg-sidebar);
      border-right: 1px solid var(--border);
      display: flex;
      flex-direction: column;
      height: 100%;
      flex-shrink: 0;
    }
    .brand {
      padding: 24px;
      display: flex;
      align-items: center;
      gap: 12px;
      border-bottom: 1px solid var(--border);
    }
    .logo-icon {
      font-size: 28px;
      color: var(--primary);
    }
    .brand-name {
      font-size: 1.25rem;
      font-weight: 700;
      letter-spacing: -0.02em;
    }
    .nav-links {
      padding: 20px 16px;
      display: flex;
      flex-direction: column;
      gap: 6px;
      flex-grow: 1;
    }
    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 14px;
      color: var(--text-muted);
      font-size: 0.95rem;
      font-weight: 500;
      border-radius: 8px;
      transition: all var(--transition-fast);
    }
    .nav-item:hover {
      background-color: #f1f5f9;
      color: var(--text-main);
    }
    .nav-item.active {
      background-color: var(--primary-light);
      color: var(--primary);
      font-weight: 600;
    }
    .sidebar-footer {
      padding: 20px 16px;
      border-top: 1px solid var(--border);
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      background-color: #fafbfc;
      min-width: 0;
    }
    .user-profile {
      display: flex;
      align-items: center;
      gap: 12px;
      min-width: 0;
      flex-grow: 1;
    }
    .user-info {
      min-width: 0;
      flex-grow: 1;
    }
    .user-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--text-main);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 130px;
    }
    .user-role {
      font-size: 0.75rem;
      color: var(--text-light);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 130px;
    }
    .logout-btn {
      background: none;
      border: none;
      color: var(--text-muted);
      cursor: pointer;
      padding: 8px;
      border-radius: 6px;
      transition: all var(--transition-fast);
      display: flex;
      align-items: center;
      flex-shrink: 0;
    }
    .logout-btn:hover {
      background-color: var(--danger-bg);
      color: var(--danger);
    }

    /* Main Panel */
    .main-panel {
      flex-grow: 1;
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
      min-width: 0;
    }
    .header {
      height: 70px;
      border-bottom: 1px solid var(--border);
      background-color: #ffffff;
      padding: 0 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-shrink: 0;
    }
    .page-title {
      font-size: 1.25rem;
      font-weight: 600;
    }
    .welcome-text {
      font-size: 0.9rem;
      color: var(--text-muted);
    }
    .content-container {
      flex-grow: 1;
      padding: 24px;
      overflow-y: auto;
      min-height: 0;
    }

    /* Hamburger Menu Button */
    .hamburger-btn {
      display: none;
      background: none;
      border: none;
      color: var(--text-main);
      cursor: pointer;
      padding: 8px;
      border-radius: 8px;
      align-items: center;
      justify-content: center;
      transition: background-color var(--transition-fast);
    }
    .hamburger-btn:hover {
      background-color: #f1f5f9;
    }
    .hamburger-btn span {
      font-size: 24px;
    }

    /* Mobile Responsive Sidebar Styles */
    @media (max-width: 768px) {
      .hamburger-btn {
        display: inline-flex;
      }
      .sidebar {
        position: fixed;
        left: 0;
        top: 0;
        bottom: 0;
        z-index: 1010;
        transform: translateX(-100%);
        transition: transform var(--transition-normal);
        box-shadow: var(--shadow-lg);
      }
      .sidebar.open {
        transform: translateX(0);
      }
      .sidebar-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(15, 23, 42, 0.3);
        backdrop-filter: blur(4px);
        z-index: 1005;
      }
      .header {
        padding: 0 16px !important;
        height: 60px !important;
      }
      .welcome-text {
        display: none !important;
      }
    }
  `]
})
export class MainLayout {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected showQuickMenu = signal(false);
  protected sidebarOpen = signal(false);

  toggleSidebar() {
    this.sidebarOpen.set(!this.sidebarOpen());
  }

  closeSidebar() {
    this.sidebarOpen.set(false);
  }

  toggleQuickMenu() {
    this.showQuickMenu.set(!this.showQuickMenu());
  }

  canCreateProject(): boolean {
    const user = this.authService.currentUser();
    return user?.roles?.includes('Admin') || user?.roles?.includes('ProjectManager');
  }

  quickCreateProject() {
    this.showQuickMenu.set(false);
    this.closeSidebar();
    this.router.navigate(['/projects'], { queryParams: { create: 'true' } });
  }

  quickCreateTask() {
    this.showQuickMenu.set(false);
    this.closeSidebar();
    const url = this.router.url;
    // Check if we are currently on a project details page
    const projectMatch = url.match(/^\/projects\/([a-f0-9-]{36})/i);
    if (projectMatch) {
      const projectId = projectMatch[1];
      this.router.navigate([`/projects/${projectId}`], { queryParams: { createTask: 'true' } });
    } else {
      this.toastService.info('Please open a specific project to create a task.');
      this.router.navigate(['/projects']);
    }
  }

  isAdmin(): boolean {
    const user = this.authService.currentUser();
    return user?.roles?.includes('Admin') ?? false;
  }

  getUserName(): string {
    return this.authService.currentUser()?.fullName ?? 'User';
  }

  getUserAvatar(): string | null {
    return this.authService.currentUser()?.avatarUrl ?? null;
  }

  getPageTitle(): string {
    const url = this.router.url;
    if (url.includes('dashboard')) return 'Overview';
    if (url.includes('my-tasks')) return 'My Tasks';
    if (url.includes('projects/')) return 'Project Details';
    if (url.includes('projects')) return 'Projects';
    if (url.includes('reports')) return 'Reports';
    if (url.includes('users')) return 'User Management';
    return 'Task Management';
  }

  onLogout() {
    this.authService.logout().subscribe({
      next: () => {
        this.toastService.success('Logged out successfully.');
        this.router.navigate(['/login']);
      },
      error: () => {
        // Fallback clear
        this.authService.clearLocalSession();
        this.router.navigate(['/login']);
      }
    });
  }
}


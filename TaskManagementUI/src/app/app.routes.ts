import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register').then(m => m.Register)
  },
  {
    path: '',
    loadComponent: () => import('./shared/components/main-layout').then(m => m.MainLayout),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard)
      },
      {
        path: 'my-tasks',
        loadComponent: () => import('./features/dashboard/my-tasks').then(m => m.MyTasks)
      },
      {
        path: 'projects',
        loadComponent: () => import('./features/projects/projects').then(m => m.Projects)
      },
      {
        path: 'projects/:id',
        loadComponent: () => import('./features/projects/project-detail').then(m => m.ProjectDetail)
      },
      {
        path: 'users',
        loadComponent: () => import('./features/auth/users').then(m => m.Users),
        canActivate: [roleGuard(['Admin'])]
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports').then(m => m.Reports)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

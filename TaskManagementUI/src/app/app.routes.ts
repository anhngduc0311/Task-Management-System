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
      },
      {
        path: 'products',
        loadComponent: () => import('./features/products/products').then(m => m.Products)
      },
      {
        path: 'products/new',
        loadComponent: () => import('./features/products/product-form').then(m => m.ProductForm)
      },
      {
        path: 'products/edit/:id',
        loadComponent: () => import('./features/products/product-form').then(m => m.ProductForm)
      },
      {
        path: 'product-categories',
        loadComponent: () => import('./features/products/categories').then(m => m.Categories)
      },
      {
        path: 'product-suppliers',
        loadComponent: () => import('./features/products/suppliers').then(m => m.Suppliers)
      },
      {
        path: 'product-units',
        loadComponent: () => import('./features/products/units').then(m => m.Units)
      },
      {
        path: 'product-origins',
        loadComponent: () => import('./features/products/origins').then(m => m.Origins)
      },
      {
        path: 'product-labels',
        loadComponent: () => import('./features/products/labels').then(m => m.Labels)
      },
      {
        path: 'warehouses',
        loadComponent: () => import('./features/inventory/warehouses').then(m => m.Warehouses)
      },
      {
        path: 'inventory-receipts',
        loadComponent: () => import('./features/inventory/receipts').then(m => m.Receipts)
      },
      {
        path: 'inventory-receipts/new',
        loadComponent: () => import('./features/inventory/receipt-form').then(m => m.ReceiptForm)
      },
      {
        path: 'inventory-receipts/edit/:id',
        loadComponent: () => import('./features/inventory/receipt-form').then(m => m.ReceiptForm)
      },
      {
        path: 'stock-balances',
        loadComponent: () => import('./features/inventory/stock-balances').then(m => m.StockBalances)
      },
      {
        path: 'stock-movements',
        loadComponent: () => import('./features/inventory/stock-movements').then(m => m.StockMovements)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

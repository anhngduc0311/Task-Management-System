import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const router = inject(Router);
    const userStr = localStorage.getItem('currentUser');
    
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        const roles = user.roles || [];
        const hasRole = allowedRoles.some(r => roles.includes(r));
        if (hasRole) {
          return true;
        }
      } catch (e) {
        // Parse error, ignore
      }
    }
    
    router.navigate(['/dashboard']);
    return false;
  };
};

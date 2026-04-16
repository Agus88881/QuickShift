import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login';
import { DashboardComponent } from './features/dashboard/dashboard';

export const routes: Routes = [
  { path: ':tenantName/login', component: LoginComponent },
  { path: ':tenantName/dashboard', component: DashboardComponent },

  { path: '', redirectTo: 'demo/login', pathMatch: 'full' },
  { path: '**', redirectTo: 'demo/login' } 
];
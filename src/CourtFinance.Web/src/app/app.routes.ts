import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { DisbursementsComponent } from './disbursements/disbursements.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'disbursements', component: DisbursementsComponent }
];

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FinanceApiService, DashboardSummary } from '../finance-api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(FinanceApiService);

  summary: DashboardSummary | null = null;
  error: string | null = null;
  loading = true;

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: (s) => {
        this.summary = s;
        this.loading = false;
      },
      error: () => {
        this.error =
          'Could not load the finance dashboard. Confirm SQL Server has the CourtFinance database (see database/CourtFinance.sql) and the API is running on port 5244.';
        this.loading = false;
      }
    });
  }

  statusEntries(): { key: string; value: number }[] {
    if (!this.summary) return [];
    return Object.entries(this.summary.disbursementCountsByStatus).map(([key, value]) => ({
      key,
      value
    }));
  }

  utilizationPercent(): number | null {
    if (!this.summary || this.summary.totalAppropriated <= 0) return null;
    return Math.round(
      (this.summary.totalPaidDisbursements / this.summary.totalAppropriated) * 1000
    ) / 10;
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DashboardSummary {
  fiscalYearLabel: string;
  totalAppropriated: number;
  totalPaidDisbursements: number;
  outstandingCommitted: number;
  disbursementCountsByStatus: Record<string, number>;
}

export interface Department {
  id: number;
  code: string;
  name: string;
}

export interface Fund {
  id: number;
  code: string;
  name: string;
}

export interface GlAccount {
  id: number;
  accountNumber: string;
  description: string;
}

export interface Vendor {
  id: number;
  code: string;
  name: string;
}

export interface CourtCase {
  id: number;
  caseNumber: string;
  title: string;
}

export interface FiscalYear {
  id: number;
  year: number;
  label: string;
  isClosed: boolean;
}

export interface DisbursementRow {
  id: number;
  departmentCode: string;
  vendorName: string;
  caseNumber: string | null;
  amount: number;
  description: string;
  requestDate: string;
  status: string;
}

export type DisbursementWorkflowStatus = 'Draft' | 'Submitted' | 'Approved' | 'Paid';

export interface CreateDisbursementPayload {
  fiscalYearId: number;
  departmentId: number;
  fundId: number;
  glAccountId: number;
  courtCaseId: number | null;
  vendorId: number;
  amount: number;
  description: string;
  requestDate: string;
}

@Injectable({ providedIn: 'root' })
export class FinanceApiService {
  private readonly http = inject(HttpClient);

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>('/api/dashboard/summary');
  }

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>('/api/departments');
  }

  getFunds(): Observable<Fund[]> {
    return this.http.get<Fund[]>('/api/funds');
  }

  getGlAccounts(): Observable<GlAccount[]> {
    return this.http.get<GlAccount[]>('/api/gl-accounts');
  }

  getVendors(): Observable<Vendor[]> {
    return this.http.get<Vendor[]>('/api/vendors');
  }

  getCourtCases(): Observable<CourtCase[]> {
    return this.http.get<CourtCase[]>('/api/court-cases');
  }

  getFiscalYears(): Observable<FiscalYear[]> {
    return this.http.get<FiscalYear[]>('/api/fiscal-years');
  }

  getDisbursements(): Observable<DisbursementRow[]> {
    return this.http.get<DisbursementRow[]>('/api/disbursements');
  }

  createDisbursement(body: CreateDisbursementPayload): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/disbursements', body);
  }

  updateDisbursementStatus(id: number, status: DisbursementWorkflowStatus): Observable<void> {
    return this.http.patch<void>(`/api/disbursements/${id}/status`, { status });
  }
}

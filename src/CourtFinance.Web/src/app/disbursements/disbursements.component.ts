import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  CourtCase,
  Department,
  DisbursementRow,
  DisbursementWorkflowStatus,
  FinanceApiService,
  FiscalYear,
  Fund,
  GlAccount,
  Vendor
} from '../finance-api.service';

@Component({
  selector: 'app-disbursements',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './disbursements.component.html',
  styleUrl: './disbursements.component.css'
})
export class DisbursementsComponent implements OnInit {
  private readonly api = inject(FinanceApiService);

  rows: DisbursementRow[] = [];
  departments: Department[] = [];
  funds: Fund[] = [];
  glAccounts: GlAccount[] = [];
  vendors: Vendor[] = [];
  courtCases: CourtCase[] = [];
  fiscalYears: FiscalYear[] = [];

  formFiscalYearId: number | null = null;
  formDepartmentId: number | null = null;
  formFundId: number | null = null;
  formGlAccountId: number | null = null;
  formCourtCaseId: number | null = null;
  formVendorId: number | null = null;
  formAmount: number | null = null;
  formDescription = '';
  formRequestDate = new Date().toISOString().slice(0, 10);

  listError: string | null = null;
  formMessage: string | null = null;
  formError: string | null = null;

  ngOnInit(): void {
    this.reloadLists();
  }

  reloadLists(): void {
    this.listError = null;
    this.api.getDisbursements().subscribe({
      next: (r) => (this.rows = r),
      error: () => (this.listError = 'Unable to load disbursements from the API.')
    });

    this.api.getDepartments().subscribe((d) => (this.departments = d));
    this.api.getFunds().subscribe((f) => (this.funds = f));
    this.api.getGlAccounts().subscribe((a) => (this.glAccounts = a));
    this.api.getVendors().subscribe((v) => (this.vendors = v));
    this.api.getCourtCases().subscribe((c) => (this.courtCases = c));
    this.api.getFiscalYears().subscribe((y) => {
      this.fiscalYears = y;
      const open = y.find((f) => !f.isClosed) ?? y[0];
      if (open && this.formFiscalYearId === null) {
        this.formFiscalYearId = open.id;
      }
    });
  }

  submit(): void {
    this.formMessage = null;
    this.formError = null;

    if (
      this.formFiscalYearId === null ||
      this.formDepartmentId === null ||
      this.formFundId === null ||
      this.formGlAccountId === null ||
      this.formVendorId === null ||
      !this.formAmount ||
      this.formAmount <= 0 ||
      !this.formDescription.trim()
    ) {
      this.formError = 'Please complete all required fields with valid values.';
      return;
    }

    this.api
      .createDisbursement({
        fiscalYearId: this.formFiscalYearId,
        departmentId: this.formDepartmentId,
        fundId: this.formFundId,
        glAccountId: this.formGlAccountId,
        courtCaseId: this.formCourtCaseId,
        vendorId: this.formVendorId,
        amount: this.formAmount,
        description: this.formDescription.trim(),
        requestDate: this.formRequestDate
      })
      .subscribe({
        next: () => {
          this.formMessage = 'Disbursement draft saved.';
          this.formDescription = '';
          this.formAmount = null;
          this.formCourtCaseId = null;
          this.reloadLists();
        },
        error: (err) => {
          const body = err?.error as { message?: string } | undefined;
          this.formError =
            typeof body?.message === 'string' ? body.message : 'Save failed.';
        }
      });
  }

  advance(row: DisbursementRow, to: DisbursementWorkflowStatus): void {
    this.api.updateDisbursementStatus(row.id, to).subscribe({
      next: () => this.reloadLists(),
      error: () => {
        this.listError = 'That status change is not allowed from the current state.';
      }
    });
  }

  actionsFor(row: DisbursementRow): { label: string; next: DisbursementWorkflowStatus }[] {
    switch (row.status) {
      case 'Draft':
        return [{ label: 'Submit', next: 'Submitted' }];
      case 'Submitted':
        return [
          { label: 'Approve', next: 'Approved' },
          { label: 'Return to draft', next: 'Draft' }
        ];
      case 'Approved':
        return [
          { label: 'Mark paid', next: 'Paid' },
          { label: 'Reopen', next: 'Submitted' }
        ];
      default:
        return [];
    }
  }
}

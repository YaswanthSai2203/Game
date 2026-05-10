namespace CourtFinance.Api.Data;

public class Disbursement
{
    public int Id { get; set; }
    public int FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int FundId { get; set; }
    public Fund Fund { get; set; } = null!;
    public int GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = null!;
    public int? CourtCaseId { get; set; }
    public CourtCase? CourtCase { get; set; }
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly RequestDate { get; set; }
    public DisbursementStatus Status { get; set; }
}

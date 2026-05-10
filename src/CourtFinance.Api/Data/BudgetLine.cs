namespace CourtFinance.Api.Data;

public class BudgetLine
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
    public decimal AppropriatedAmount { get; set; }
}

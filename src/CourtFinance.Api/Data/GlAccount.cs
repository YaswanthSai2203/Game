namespace CourtFinance.Api.Data;

public class GlAccount
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

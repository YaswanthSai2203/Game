namespace CourtFinance.Api.Data;

public class Fund
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

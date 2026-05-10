namespace CourtFinance.Api.Data;

public class FiscalYear
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsClosed { get; set; }

    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

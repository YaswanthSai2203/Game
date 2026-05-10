namespace CourtFinance.Api.Data;

public class CourtCase
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

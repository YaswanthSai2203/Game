namespace CourtFinance.Api.Data;

public class Vendor
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

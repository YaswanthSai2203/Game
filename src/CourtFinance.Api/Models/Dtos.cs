using CourtFinance.Api.Data;

namespace CourtFinance.Api.Models;

public record ErrorResponse(string Message);

public record DashboardSummaryResponse(
    string FiscalYearLabel,
    decimal TotalAppropriated,
    decimal TotalPaidDisbursements,
    decimal OutstandingCommitted,
    IReadOnlyDictionary<string, int> DisbursementCountsByStatus);

public record DepartmentDto(int Id, string Code, string Name);

public record FundDto(int Id, string Code, string Name);

public record GlAccountDto(int Id, string AccountNumber, string Description);

public record VendorDto(int Id, string Code, string Name);

public record CourtCaseDto(int Id, string CaseNumber, string Title);

public record DisbursementListItemDto(
    int Id,
    string DepartmentCode,
    string VendorName,
    string? CaseNumber,
    decimal Amount,
    string Description,
    DateOnly RequestDate,
    string Status);

public record CreateDisbursementRequest(
    int FiscalYearId,
    int DepartmentId,
    int FundId,
    int GlAccountId,
    int? CourtCaseId,
    int VendorId,
    decimal Amount,
    string Description,
    DateOnly RequestDate);

public record PatchDisbursementStatusRequest(DisbursementStatus Status);

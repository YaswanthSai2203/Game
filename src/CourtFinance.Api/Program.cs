using System.Text.Json;
using System.Text.Json.Serialization;
using CourtFinance.Api.Data;
using CourtFinance.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var connectionString = builder.Configuration.GetConnectionString("CourtFinanceDatabase")
    ?? throw new InvalidOperationException("Connection string 'CourtFinanceDatabase' is not configured.");

builder.Services.AddDbContext<CourtFinanceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4300"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/dashboard/summary", Task<Results<Ok<DashboardSummaryResponse>, NotFound<ErrorResponse>>> (CourtFinanceDbContext db) =>
{
    var openYear = await db.FiscalYears.AsNoTracking().FirstOrDefaultAsync(f => !f.IsClosed)
                   ?? await db.FiscalYears.AsNoTracking().OrderByDescending(f => f.Year).FirstOrDefaultAsync();

    if (openYear is null)
        return TypedResults.NotFound(new ErrorResponse("No fiscal years are configured. Run database/CourtFinance.sql."));

    var fyId = openYear.Id;

    var totalAppropriated = await db.BudgetLines.AsNoTracking()
        .Where(b => b.FiscalYearId == fyId)
        .SumAsync(b => b.AppropriatedAmount);

    var disbursements = await db.Disbursements.AsNoTracking()
        .Where(d => d.FiscalYearId == fyId)
        .Select(d => new { d.Status, d.Amount })
        .ToListAsync();

    var totalPaid = disbursements.Where(d => d.Status == DisbursementStatus.Paid).Sum(d => d.Amount);
    var outstanding = disbursements
        .Where(d => d.Status is DisbursementStatus.Submitted or DisbursementStatus.Approved)
        .Sum(d => d.Amount);

    var counts = Enum.GetValues<DisbursementStatus>()
        .ToDictionary(s => s.ToString(), s => disbursements.Count(d => d.Status == s));

    var response = new DashboardSummaryResponse(
        openYear.Label,
        totalAppropriated,
        totalPaid,
        outstanding,
        counts);

    return TypedResults.Ok(response);
});

api.MapGet("/departments", async (CourtFinanceDbContext db) =>
{
    var rows = await db.Departments.AsNoTracking()
        .OrderBy(d => d.Code)
        .Select(d => new DepartmentDto(d.Id, d.Code, d.Name))
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/funds", async (CourtFinanceDbContext db) =>
{
    var rows = await db.Funds.AsNoTracking()
        .OrderBy(f => f.Code)
        .Select(f => new FundDto(f.Id, f.Code, f.Name))
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/gl-accounts", async (CourtFinanceDbContext db) =>
{
    var rows = await db.GlAccounts.AsNoTracking()
        .OrderBy(a => a.AccountNumber)
        .Select(a => new GlAccountDto(a.Id, a.AccountNumber, a.Description))
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/vendors", async (CourtFinanceDbContext db) =>
{
    var rows = await db.Vendors.AsNoTracking()
        .OrderBy(v => v.Code)
        .Select(v => new VendorDto(v.Id, v.Code, v.Name))
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/court-cases", async (CourtFinanceDbContext db) =>
{
    var rows = await db.CourtCases.AsNoTracking()
        .OrderBy(c => c.CaseNumber)
        .Select(c => new CourtCaseDto(c.Id, c.CaseNumber, c.Title))
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/fiscal-years", async (CourtFinanceDbContext db) =>
{
    var rows = await db.FiscalYears.AsNoTracking()
        .OrderByDescending(f => f.Year)
        .Select(f => new { f.Id, f.Year, f.Label, f.IsClosed })
        .ToListAsync();
    return Results.Ok(rows);
});

api.MapGet("/disbursements", async (CourtFinanceDbContext db) =>
{
    var raw = await db.Disbursements.AsNoTracking()
        .OrderByDescending(d => d.RequestDate)
        .ThenByDescending(d => d.Id)
        .Select(d => new
        {
            d.Id,
            DepartmentCode = d.Department.Code,
            VendorName = d.Vendor.Name,
            CaseNumber = d.CourtCase != null ? d.CourtCase.CaseNumber : null,
            d.Amount,
            d.Description,
            d.RequestDate,
            d.Status
        })
        .ToListAsync();

    var rows = raw.Select(d => new DisbursementListItemDto(
            d.Id,
            d.DepartmentCode,
            d.VendorName,
            d.CaseNumber,
            d.Amount,
            d.Description,
            d.RequestDate,
            d.Status.ToString()))
        .ToList();

    return Results.Ok(rows);
});

api.MapPost("/disbursements", async (CreateDisbursementRequest body, CourtFinanceDbContext db) =>
{
    if (body.Amount <= 0)
        return Results.BadRequest(new ErrorResponse("Amount must be greater than zero."));

    var fyExists = await db.FiscalYears.AnyAsync(f => f.Id == body.FiscalYearId);
    if (!fyExists)
        return Results.BadRequest(new ErrorResponse("Invalid fiscal year."));

    var deptExists = await db.Departments.AnyAsync(d => d.Id == body.DepartmentId);
    if (!deptExists)
        return Results.BadRequest(new ErrorResponse("Invalid department."));

    var fundExists = await db.Funds.AnyAsync(f => f.Id == body.FundId);
    if (!fundExists)
        return Results.BadRequest(new ErrorResponse("Invalid fund."));

    var glExists = await db.GlAccounts.AnyAsync(a => a.Id == body.GlAccountId);
    if (!glExists)
        return Results.BadRequest(new ErrorResponse("Invalid GL account."));

    var vendorExists = await db.Vendors.AnyAsync(v => v.Id == body.VendorId);
    if (!vendorExists)
        return Results.BadRequest(new ErrorResponse("Invalid vendor."));

    if (body.CourtCaseId is { } caseId && !await db.CourtCases.AnyAsync(c => c.Id == caseId))
        return Results.BadRequest(new ErrorResponse("Invalid court case."));

    var entity = new Disbursement
    {
        FiscalYearId = body.FiscalYearId,
        DepartmentId = body.DepartmentId,
        FundId = body.FundId,
        GlAccountId = body.GlAccountId,
        CourtCaseId = body.CourtCaseId,
        VendorId = body.VendorId,
        Amount = body.Amount,
        Description = body.Description.Trim(),
        RequestDate = body.RequestDate,
        Status = DisbursementStatus.Draft
    };

    db.Disbursements.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/api/disbursements/{entity.Id}", new { entity.Id });
});

api.MapPatch("/disbursements/{id:int}/status", async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>>> (
    int id,
    PatchDisbursementStatusRequest body,
    CourtFinanceDbContext db) =>
{
    var entity = await db.Disbursements.FirstOrDefaultAsync(d => d.Id == id);
    if (entity is null)
        return TypedResults.NotFound();

    if (!IsValidTransition(entity.Status, body.Status))
        return TypedResults.BadRequest(new ErrorResponse(
            $"Cannot move from {entity.Status} to {body.Status}."));

    entity.Status = body.Status;
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
});

app.MapFallbackToFile("index.html");

app.Run();

static bool IsValidTransition(DisbursementStatus from, DisbursementStatus to)
{
    if (from == to)
        return true;

    return (from, to) switch
    {
        (DisbursementStatus.Draft, DisbursementStatus.Submitted) => true,
        (DisbursementStatus.Submitted, DisbursementStatus.Approved) => true,
        (DisbursementStatus.Submitted, DisbursementStatus.Draft) => true,
        (DisbursementStatus.Approved, DisbursementStatus.Paid) => true,
        (DisbursementStatus.Approved, DisbursementStatus.Submitted) => true,
        _ => false
    };
}

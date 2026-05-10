using Microsoft.EntityFrameworkCore;

namespace CourtFinance.Api.Data;

public class CourtFinanceDbContext : DbContext
{
    public CourtFinanceDbContext(DbContextOptions<CourtFinanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<GlAccount> GlAccounts => Set<GlAccount>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<CourtCase> CourtCases => Set<CourtCase>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Disbursement> Disbursements => Set<Disbursement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalYear>(e =>
        {
            e.Property(x => x.Label).HasMaxLength(32);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<Fund>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<GlAccount>(e =>
        {
            e.Property(x => x.AccountNumber).HasMaxLength(32);
            e.Property(x => x.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<CourtCase>(e =>
        {
            e.Property(x => x.CaseNumber).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(256);
        });

        modelBuilder.Entity<BudgetLine>(e =>
        {
            e.Property(x => x.AppropriatedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Disbursement>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Status).HasConversion<byte>();
        });
    }
}

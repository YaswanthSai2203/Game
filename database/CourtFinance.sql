-- Court Finance: on-premises SQL Server schema and sample reference data.
-- Target: SQL Server 2019+ (compatibility level 150+ recommended).

IF DB_ID(N'CourtFinance') IS NULL
    CREATE DATABASE CourtFinance;
GO

USE CourtFinance;
GO

IF OBJECT_ID(N'dbo.Disbursements', N'U') IS NOT NULL DROP TABLE dbo.Disbursements;
IF OBJECT_ID(N'dbo.BudgetLines', N'U') IS NOT NULL DROP TABLE dbo.BudgetLines;
IF OBJECT_ID(N'dbo.CourtCases', N'U') IS NOT NULL DROP TABLE dbo.CourtCases;
IF OBJECT_ID(N'dbo.Vendors', N'U') IS NOT NULL DROP TABLE dbo.Vendors;
IF OBJECT_ID(N'dbo.GlAccounts', N'U') IS NOT NULL DROP TABLE dbo.GlAccounts;
IF OBJECT_ID(N'dbo.Funds', N'U') IS NOT NULL DROP TABLE dbo.Funds;
IF OBJECT_ID(N'dbo.Departments', N'U') IS NOT NULL DROP TABLE dbo.Departments;
IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL DROP TABLE dbo.FiscalYears;
GO

CREATE TABLE dbo.FiscalYears (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Year] INT NOT NULL,
    Label NVARCHAR(32) NOT NULL,
    IsClosed BIT NOT NULL CONSTRAINT DF_FiscalYears_IsClosed DEFAULT (0)
);

CREATE TABLE dbo.Departments (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Code NVARCHAR(16) NOT NULL,
    Name NVARCHAR(128) NOT NULL
);

CREATE TABLE dbo.Funds (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Code NVARCHAR(16) NOT NULL,
    Name NVARCHAR(128) NOT NULL
);

CREATE TABLE dbo.GlAccounts (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AccountNumber NVARCHAR(32) NOT NULL,
    Description NVARCHAR(256) NOT NULL
);

CREATE TABLE dbo.Vendors (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Code NVARCHAR(16) NOT NULL,
    Name NVARCHAR(256) NOT NULL
);

CREATE TABLE dbo.CourtCases (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CaseNumber NVARCHAR(64) NOT NULL,
    Title NVARCHAR(256) NOT NULL
);

CREATE TABLE dbo.BudgetLines (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FiscalYearId INT NOT NULL,
    DepartmentId INT NOT NULL,
    FundId INT NOT NULL,
    GlAccountId INT NOT NULL,
    AppropriatedAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_BudgetLines_FiscalYears FOREIGN KEY (FiscalYearId) REFERENCES dbo.FiscalYears (Id),
    CONSTRAINT FK_BudgetLines_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_BudgetLines_Funds FOREIGN KEY (FundId) REFERENCES dbo.Funds (Id),
    CONSTRAINT FK_BudgetLines_GlAccounts FOREIGN KEY (GlAccountId) REFERENCES dbo.GlAccounts (Id)
);

-- Status: 0 Draft, 1 Submitted, 2 Approved, 3 Paid
CREATE TABLE dbo.Disbursements (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FiscalYearId INT NOT NULL,
    DepartmentId INT NOT NULL,
    FundId INT NOT NULL,
    GlAccountId INT NOT NULL,
    CourtCaseId INT NULL,
    VendorId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(512) NOT NULL,
    RequestDate DATE NOT NULL,
    Status TINYINT NOT NULL CONSTRAINT DF_Disbursements_Status DEFAULT (0),
    CONSTRAINT FK_Disbursements_FiscalYears FOREIGN KEY (FiscalYearId) REFERENCES dbo.FiscalYears (Id),
    CONSTRAINT FK_Disbursements_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id),
    CONSTRAINT FK_Disbursements_Funds FOREIGN KEY (FundId) REFERENCES dbo.Funds (Id),
    CONSTRAINT FK_Disbursements_GlAccounts FOREIGN KEY (GlAccountId) REFERENCES dbo.GlAccounts (Id),
    CONSTRAINT FK_Disbursements_CourtCases FOREIGN KEY (CourtCaseId) REFERENCES dbo.CourtCases (Id),
    CONSTRAINT FK_Disbursements_Vendors FOREIGN KEY (VendorId) REFERENCES dbo.Vendors (Id)
);

CREATE INDEX IX_Disbursements_FiscalYear_Status ON dbo.Disbursements (FiscalYearId, Status);
CREATE INDEX IX_BudgetLines_FiscalYear_Department ON dbo.BudgetLines (FiscalYearId, DepartmentId);
GO

SET IDENTITY_INSERT dbo.FiscalYears ON;
INSERT INTO dbo.FiscalYears (Id, [Year], Label, IsClosed) VALUES (1, 2026, N'FY 2026', 0);
SET IDENTITY_INSERT dbo.FiscalYears OFF;

SET IDENTITY_INSERT dbo.Departments ON;
INSERT INTO dbo.Departments (Id, Code, Name) VALUES
 (1, N'ADM', N'Court Administration'),
 (2, N'CRIM', N'Criminal Division'),
 (3, N'CIV', N'Civil Division');
SET IDENTITY_INSERT dbo.Departments OFF;

SET IDENTITY_INSERT dbo.Funds ON;
INSERT INTO dbo.Funds (Id, Code, Name) VALUES
 (1, N'GEN', N'General Fund'),
 (2, N'SPEC', N'Special Revenue');
SET IDENTITY_INSERT dbo.Funds OFF;

SET IDENTITY_INSERT dbo.GlAccounts ON;
INSERT INTO dbo.GlAccounts (Id, AccountNumber, Description) VALUES
 (1, N'5110-100', N'Professional Services'),
 (2, N'5210-200', N'Court Reporter Services'),
 (3, N'5310-300', N'Jury Costs');
SET IDENTITY_INSERT dbo.GlAccounts OFF;

SET IDENTITY_INSERT dbo.Vendors ON;
INSERT INTO dbo.Vendors (Id, Code, Name) VALUES
 (1, N'V-1001', N'County Reporting Services LLC'),
 (2, N'V-1002', N'Legal Aid Partners'),
 (3, N'V-1003', N'Facilities Maintenance Co.');
SET IDENTITY_INSERT dbo.Vendors OFF;

SET IDENTITY_INSERT dbo.CourtCases ON;
INSERT INTO dbo.CourtCases (Id, CaseNumber, Title) VALUES
 (1, N'CR-2026-0142', N'State v. Example (reporter transcript)'),
 (2, N'CV-2026-0088', N'Example Contract Dispute'),
 (3, N'CR-2026-0201', N'State v. Doe (expert witness fees)');
SET IDENTITY_INSERT dbo.CourtCases OFF;

INSERT INTO dbo.BudgetLines (FiscalYearId, DepartmentId, FundId, GlAccountId, AppropriatedAmount) VALUES
 (1, 1, 1, 1, 250000.00),
 (1, 2, 1, 2, 180000.00),
 (1, 3, 1, 3, 95000.00);

INSERT INTO dbo.Disbursements (FiscalYearId, DepartmentId, FundId, GlAccountId, CourtCaseId, VendorId, Amount, Description, RequestDate, Status) VALUES
 (1, 2, 1, 2, 1, 1, 1240.50, N'Transcript order — preliminary hearing', '2026-03-12', 3),
 (1, 3, 1, 1, 2, 2, 3500.00, N'Court-appointed counsel reimbursement', '2026-04-02', 2),
 (1, 1, 1, 3, NULL, 3, 890.25, N'HVAC service — main courthouse', '2026-04-18', 1);
GO

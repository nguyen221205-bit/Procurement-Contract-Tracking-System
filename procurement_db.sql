-- =====================================================================================
-- DATABASE SCRIPT: Procurement & Contract Tracking System
-- Tên CSDL: procurement_db
-- Hệ quản trị CSDL: Microsoft SQL Server 2019/2022/2025
-- Thiết kế: Chuẩn 3NF (16 bảng, Ràng buộc khóa ngoại, Chỉ mục tối ưu, Dữ liệu khởi tạo)
-- =====================================================================================

USE master;
GO

-- 1. Khởi tạo Database nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'procurement_db')
BEGIN
    CREATE DATABASE procurement_db;
    PRINT N'Đã tạo mới cơ sở dữ liệu procurement_db thành công.';
END
ELSE
BEGIN
    PRINT N'Cơ sở dữ liệu procurement_db đã tồn tại.';
END
GO

USE procurement_db;
GO

-- =====================================================================================
-- PHÂN HỆ 1: NGƯỜI DÙNG & NHÀ THẦU
-- =====================================================================================

-- Bảng 1: Users (Tài khoản người dùng hệ thống)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Phone NVARCHAR(20) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        RefreshToken NVARCHAR(MAX) NULL,
        RefreshTokenExpiry DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Email ON Users (Email);
    PRINT N'Đã tạo bảng Users.';
END
GO

-- Bảng 2: Roles (Danh mục vai trò phân quyền)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Roles_Name UNIQUE (Name)
    );
    PRINT N'Đã tạo bảng Roles.';
END
GO

-- Bảng 3: UserRoles (Bảng trung gian phân quyền N:N giữa Users và Roles)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE UserRoles (
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_UserRoles_RoleId ON UserRoles (RoleId);
    PRINT N'Đã tạo bảng UserRoles.';
END
GO

-- Bảng 4: Contractors (Hồ sơ Nhà thầu - Quan hệ 1:1 với Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Contractors')
BEGIN
    CREATE TABLE Contractors (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        TaxCode NVARCHAR(20) NULL,
        Address NVARCHAR(500) NULL,
        BusinessLicenseFile NVARCHAR(500) NULL,
        Rating DECIMAL(5,2) NOT NULL CONSTRAINT DF_Contractors_Rating DEFAULT 0.00,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Contractors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Contractors PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Contractors_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Contractors_UserId ON Contractors (UserId);
    PRINT N'Đã tạo bảng Contractors.';
END
GO

-- =====================================================================================
-- PHÂN HỆ 2: MỜI THẦU & HỒ SƠ DỰ THẦU
-- =====================================================================================

-- Bảng 5: BidPackages (Gói thầu mời thầu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidPackages')
BEGIN
    CREATE TABLE BidPackages (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        Type INT NOT NULL, -- 0: Goods, 1: Construction, 2: Service
        Budget DECIMAL(18,2) NOT NULL,
        Deadline DATETIME2 NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_BidPackages_Status DEFAULT 0, -- 0: Open, 1: Closed, 2: Evaluating, 3: Contracted
        Description NVARCHAR(2000) NULL,
        CreatedBy INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_BidPackages_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_BidPackages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_BidPackages_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_BidPackages_Code ON BidPackages (Code);
    CREATE NONCLUSTERED INDEX IX_BidPackages_CreatedBy ON BidPackages (CreatedBy);
    CREATE NONCLUSTERED INDEX IX_BidPackages_Status ON BidPackages (Status);
    CREATE NONCLUSTERED INDEX IX_BidPackages_Deadline ON BidPackages (Deadline);
    PRINT N'Đã tạo bảng BidPackages.';
END
GO

-- Bảng 6: BidDocuments (Tài liệu đính kèm hồ sơ mời thầu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidDocuments')
BEGIN
    CREATE TABLE BidDocuments (
        Id INT IDENTITY(1,1) NOT NULL,
        BidPackageId INT NOT NULL,
        FileName NVARCHAR(300) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        UploadedAt DATETIME2 NOT NULL CONSTRAINT DF_BidDocuments_UploadedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BidDocuments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_BidDocuments_BidPackages FOREIGN KEY (BidPackageId) REFERENCES BidPackages(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_BidDocuments_BidPackageId ON BidDocuments (BidPackageId);
    PRINT N'Đã tạo bảng BidDocuments.';
END
GO

-- Bảng 7: BidSubmissions (Hồ sơ dự thầu do nhà thầu nộp)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidSubmissions')
BEGIN
    CREATE TABLE BidSubmissions (
        Id INT IDENTITY(1,1) NOT NULL,
        BidPackageId INT NOT NULL,
        ContractorId INT NOT NULL,
        SubmittedAt DATETIME2 NOT NULL CONSTRAINT DF_BidSubmissions_SubmittedAt DEFAULT SYSUTCDATETIME(),
        TotalScore DECIMAL(10,2) NULL,
        Rank INT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_BidSubmissions_Status DEFAULT 'Submitted',
        CONSTRAINT PK_BidSubmissions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_BidSubmissions_BidPackages FOREIGN KEY (BidPackageId) REFERENCES BidPackages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BidSubmissions_Contractors FOREIGN KEY (ContractorId) REFERENCES Contractors(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_BidSubmissions_BidPackageId_ContractorId ON BidSubmissions (BidPackageId, ContractorId);
    CREATE NONCLUSTERED INDEX IX_BidSubmissions_ContractorId ON BidSubmissions (ContractorId);
    PRINT N'Đã tạo bảng BidSubmissions.';
END
GO

-- Bảng 8: SubmissionFiles (Tệp tài liệu đính kèm hồ sơ dự thầu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubmissionFiles')
BEGIN
    CREATE TABLE SubmissionFiles (
        Id INT IDENTITY(1,1) NOT NULL,
        BidSubmissionId INT NOT NULL,
        FileType INT NOT NULL, -- 0: Quotation, 1: Capability, 2: Schedule, 3: Other
        FileName NVARCHAR(300) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        UploadedAt DATETIME2 NOT NULL CONSTRAINT DF_SubmissionFiles_UploadedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_SubmissionFiles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SubmissionFiles_BidSubmissions FOREIGN KEY (BidSubmissionId) REFERENCES BidSubmissions(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_SubmissionFiles_BidSubmissionId ON SubmissionFiles (BidSubmissionId);
    PRINT N'Đã tạo bảng SubmissionFiles.';
END
GO

-- =====================================================================================
-- PHÂN HỆ 3: TIÊU CHÍ & ĐÁNH GIÁ CHẤM ĐIỂM
-- =====================================================================================

-- Bảng 9: EvaluationCriteria (Tiêu chí chấm điểm của gói thầu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluationCriteria')
BEGIN
    CREATE TABLE EvaluationCriteria (
        Id INT IDENTITY(1,1) NOT NULL,
        BidPackageId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        MaxScore DECIMAL(5,2) NOT NULL,
        Weight DECIMAL(5,2) NOT NULL CONSTRAINT DF_EvaluationCriteria_Weight DEFAULT 1.00,
        CONSTRAINT PK_EvaluationCriteria PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_EvaluationCriteria_BidPackages FOREIGN KEY (BidPackageId) REFERENCES BidPackages(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_EvaluationCriteria_BidPackageId ON EvaluationCriteria (BidPackageId);
    PRINT N'Đã tạo bảng EvaluationCriteria.';
END
GO

-- Bảng 10: EvaluationScores (Bảng điểm chấm chi tiết từng hồ sơ)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluationScores')
BEGIN
    CREATE TABLE EvaluationScores (
        Id INT IDENTITY(1,1) NOT NULL,
        BidSubmissionId INT NOT NULL,
        CriteriaId INT NOT NULL,
        EvaluatorId INT NOT NULL,
        Score DECIMAL(5,2) NOT NULL,
        Comment NVARCHAR(500) NULL,
        ScoredAt DATETIME2 NOT NULL CONSTRAINT DF_EvaluationScores_ScoredAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_EvaluationScores PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_EvaluationScores_BidSubmissions FOREIGN KEY (BidSubmissionId) REFERENCES BidSubmissions(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EvaluationScores_EvaluationCriteria FOREIGN KEY (CriteriaId) REFERENCES EvaluationCriteria(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EvaluationScores_Users_Evaluator FOREIGN KEY (EvaluatorId) REFERENCES Users(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_EvaluationScores_BidSubmissionId_CriteriaId_EvaluatorId 
        ON EvaluationScores (BidSubmissionId, CriteriaId, EvaluatorId);
    CREATE NONCLUSTERED INDEX IX_EvaluationScores_CriteriaId ON EvaluationScores (CriteriaId);
    CREATE NONCLUSTERED INDEX IX_EvaluationScores_EvaluatorId ON EvaluationScores (EvaluatorId);
    PRINT N'Đã tạo bảng EvaluationScores.';
END
GO

-- =====================================================================================
-- PHÂN HỆ 4: HỢP ĐỒNG, TIẾN ĐỘ & NGHIỆM THU
-- =====================================================================================

-- Bảng 11: Contracts (Hợp đồng kinh tế đã ký kết)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Contracts')
BEGIN
    CREATE TABLE Contracts (
        Id INT IDENTITY(1,1) NOT NULL,
        BidPackageId INT NOT NULL,
        ContractorId INT NOT NULL,
        ContractNumber NVARCHAR(50) NOT NULL,
        Value DECIMAL(18,2) NOT NULL,
        Terms NVARCHAR(3000) NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT 0, -- 0: Draft, 1: Active, 2: Completed, 3: Terminated
        ScannedFilePath NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_Contracts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Contracts_BidPackages FOREIGN KEY (BidPackageId) REFERENCES BidPackages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Contracts_Contractors FOREIGN KEY (ContractorId) REFERENCES Contractors(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Contracts_BidPackageId ON Contracts (BidPackageId);
    CREATE UNIQUE NONCLUSTERED INDEX IX_Contracts_ContractNumber ON Contracts (ContractNumber);
    CREATE NONCLUSTERED INDEX IX_Contracts_ContractorId ON Contracts (ContractorId);
    CREATE NONCLUSTERED INDEX IX_Contracts_Status ON Contracts (Status);
    CREATE NONCLUSTERED INDEX IX_Contracts_EndDate ON Contracts (EndDate);
    PRINT N'Đã tạo bảng Contracts.';
END
GO

-- Bảng 12: ContractMilestones (Mốc thanh toán và kế hoạch giải ngân)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContractMilestones')
BEGIN
    CREATE TABLE ContractMilestones (
        Id INT IDENTITY(1,1) NOT NULL,
        ContractId INT NOT NULL,
        Title NVARCHAR(300) NOT NULL,
        DueDate DATETIME2 NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_ContractMilestones_Status DEFAULT 0, -- 0: Pending, 1: InProgress, 2: Completed, 3: Overdue
        CONSTRAINT PK_ContractMilestones PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ContractMilestones_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_ContractMilestones_ContractId ON ContractMilestones (ContractId);
    PRINT N'Đã tạo bảng ContractMilestones.';
END
GO

-- Bảng 13: ProgressUpdates (Báo cáo tiến độ tuần của nhà thầu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProgressUpdates')
BEGIN
    CREATE TABLE ProgressUpdates (
        Id INT IDENTITY(1,1) NOT NULL,
        ContractId INT NOT NULL,
        WeekNumber INT NOT NULL,
        CompletionPercent DECIMAL(5,2) NOT NULL,
        Note NVARCHAR(2000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProgressUpdates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProgressUpdates PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ProgressUpdates_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_ProgressUpdates_ContractId_WeekNumber ON ProgressUpdates (ContractId, WeekNumber);
    PRINT N'Đã tạo bảng ProgressUpdates.';
END
GO

-- Bảng 14: Acceptances (Biên bản nghiệm thu từng mốc / giai đoạn)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Acceptances')
BEGIN
    CREATE TABLE Acceptances (
        Id INT IDENTITY(1,1) NOT NULL,
        ContractId INT NOT NULL,
        MilestoneId INT NULL,
        ApprovedBy INT NOT NULL,
        ApprovedAt DATETIME2 NULL,
        Status INT NOT NULL CONSTRAINT DF_Acceptances_Status DEFAULT 0, -- 0: Pending, 1: Approved, 2: Rejected
        Note NVARCHAR(1000) NULL,
        CONSTRAINT PK_Acceptances PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Acceptances_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Acceptances_ContractMilestones FOREIGN KEY (MilestoneId) REFERENCES ContractMilestones(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Acceptances_Users_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(Id) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX IX_Acceptances_ContractId ON Acceptances (ContractId);
    CREATE NONCLUSTERED INDEX IX_Acceptances_MilestoneId ON Acceptances (MilestoneId);
    CREATE NONCLUSTERED INDEX IX_Acceptances_ApprovedBy ON Acceptances (ApprovedBy);
    PRINT N'Đã tạo bảng Acceptances.';
END
GO

-- =====================================================================================
-- PHÂN HỆ 5: GIÁM SÁT, THÔNG BÁO & NHẬT KÝ KIỂM TOÁN
-- =====================================================================================

-- Bảng 15: Notifications (Thông báo & Cảnh báo hệ thống)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
        Type INT NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT 0, -- 0: Info, 1: Warning, 2: ContractExpiring, 3: DeadlineApproaching, 4: ProgressOverdue
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_Notifications_UserId ON Notifications (UserId);
    CREATE NONCLUSTERED INDEX IX_Notifications_IsRead ON Notifications (IsRead);
    PRINT N'Đã tạo bảng Notifications.';
END
GO

-- Bảng 16: AuditLogs (Nhật ký kiểm toán toàn bộ thao tác hệ thống)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId INT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId INT NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT SYSUTCDATETIME(),
        IpAddress NVARCHAR(50) NULL,
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON AuditLogs (UserId);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityType ON AuditLogs (EntityType);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON AuditLogs (Timestamp);
    PRINT N'Đã tạo bảng AuditLogs.';
END
GO

-- =====================================================================================
-- SEED DATA (DỮ LIỆU BAN ĐẦU)
-- =====================================================================================

-- 1. Seed Roles
IF NOT EXISTS (SELECT 1 FROM Roles)
BEGIN
    INSERT INTO Roles (Name, Description) VALUES
    (N'Admin', N'Quản trị viên hệ thống có toàn quyền'),
    (N'Procurement', N'Bộ phận mua sắm - Quản lý gói thầu, hợp đồng, nghiệm thu'),
    (N'Evaluator', N'Ban chấm điểm - Cấu hình tiêu chí, chấm điểm hồ sơ dự thầu'),
    (N'Contractor', N'Nhà thầu - Đăng ký hồ sơ, nộp dự thầu, cập nhật tiến độ tuần');
    PRINT N'Đã nạp 4 vai trò mặc định vào bảng Roles.';
END
GO

-- 2. Seed Admin User (Mật khẩu: Admin@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'admin@procurement.com')
BEGIN
    -- Password hash của 'Admin@123' qua BCrypt
    INSERT INTO Users (FullName, Email, PasswordHash, Phone, IsActive, CreatedAt)
    VALUES (
        N'System Administrator',
        N'admin@procurement.com',
        N'$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi',
        N'0900000000',
        1,
        SYSUTCDATETIME()
    );

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Admin');

    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@AdminUserId, @AdminRoleId);

    PRINT N'Đã tạo tài khoản quản trị mặc định: admin@procurement.com / Admin@123';
END
GO

PRINT N'===========================================================';
PRINT N'HOÀN TẤT TẠO CƠ SỞ DỮ LIỆU PROCUREMENT_DB CHUẨN 3NF!';
PRINT N'===========================================================';
GO

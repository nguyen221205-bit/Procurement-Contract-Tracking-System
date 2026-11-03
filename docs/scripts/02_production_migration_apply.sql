-- ==============================================================================
-- SCRIPT 02: PRODUCTION MIGRATION APPLICATION (IDEMPOTENT)
-- Target Database: procurement_db (Production)
-- Migrations Applied:
--   1. 20261028094453_AddTransparencyControlsIntegrityFixes
--   2. 20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency
--   3. 20261029173000_AddBidPackageProcuringEntityOwnership
--   4. 20261030120000_QuarantineUnprovenPackageOwnership
-- ==============================================================================

USE [procurement_db];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN

                        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BidPackageEvaluators_Users_EvaluatorId')
                            ALTER TABLE [BidPackageEvaluators] DROP CONSTRAINT [FK_BidPackageEvaluators_Users_EvaluatorId];
                        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BidPackageEvaluators_Users_Evaluator')
                            ALTER TABLE [BidPackageEvaluators] DROP CONSTRAINT [FK_BidPackageEvaluators_Users_Evaluator];
                    
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [ContractMilestones] ADD [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [ContractMilestones] ADD [Version] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Acceptances]') AND [c].[name] = N'ApprovedBy');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Acceptances] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Acceptances] ALTER COLUMN [ApprovedBy] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [MilestoneVersion] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [Revision] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SigningAuthorityId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SnapshotAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SnapshotDocumentHash] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SnapshotDocumentPath] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SnapshotDueDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD [SnapshotTitle] nvarchar(300) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE TABLE [SigningAuthorities] (
        [Id] int NOT NULL IDENTITY,
        [ProcuringEntityId] int NOT NULL,
        [UserId] int NOT NULL,
        [AuthorityType] int NOT NULL,
        [Scope] nvarchar(100) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [DecisionNumber] nvarchar(100) NOT NULL,
        [DocumentUrl] nvarchar(500) NULL,
        [LegalBasis] nvarchar(500) NOT NULL,
        [RequestedByUserId] int NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [VerifiedByUserId] int NULL,
        [VerifiedAt] datetime2 NULL,
        [VerificationNotes] nvarchar(1000) NULL,
        [RevokedByUserId] int NULL,
        [RevokedAt] datetime2 NULL,
        [RevocationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SigningAuthorities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SigningAuthorities_ProcuringEntities_ProcuringEntityId] FOREIGN KEY ([ProcuringEntityId]) REFERENCES [ProcuringEntities] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SigningAuthorities_Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SigningAuthorities_Users_RevokedByUserId] FOREIGN KEY ([RevokedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SigningAuthorities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SigningAuthorities_Users_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_Acceptances_SigningAuthorityId] ON [Acceptances] ([SigningAuthorityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_ProcuringEntityId_UserId_Status] ON [SigningAuthorities] ([ProcuringEntityId], [UserId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_RequestedByUserId] ON [SigningAuthorities] ([RequestedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_RevokedByUserId] ON [SigningAuthorities] ([RevokedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_Status] ON [SigningAuthorities] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_UserId] ON [SigningAuthorities] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    CREATE INDEX [IX_SigningAuthorities_VerifiedByUserId] ON [SigningAuthorities] ([VerifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [Acceptances] ADD CONSTRAINT [FK_Acceptances_SigningAuthorities_SigningAuthorityId] FOREIGN KEY ([SigningAuthorityId]) REFERENCES [SigningAuthorities] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    ALTER TABLE [BidPackageEvaluators] ADD CONSTRAINT [FK_BidPackageEvaluators_Users_EvaluatorId] FOREIGN KEY ([EvaluatorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN

                    UPDATE [ContractMilestones] SET [Version] = 1 WHERE [Version] = 0;
                    UPDATE [Acceptances] SET [MilestoneVersion] = 1 WHERE [MilestoneVersion] = 0;
                    UPDATE [Acceptances] SET [Revision] = 1 WHERE [Revision] = 0;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028094453_AddTransparencyControlsIntegrityFixes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028094453_AddTransparencyControlsIntegrityFixes', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency'
)
BEGIN
    ALTER TABLE [ProcuringEntityMembers] ADD [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency'
)
BEGIN

                    -- 1. Đảm bảo mọi mốc thanh toán có Version >= 1
                    UPDATE [ContractMilestones] SET [Version] = 1 WHERE [Version] <= 0 OR [Version] IS NULL;

                    -- 2. Đảm bảo mọi Acceptance có MilestoneVersion >= 1 đồng bộ theo mốc hiện hành
                    UPDATE a
                    SET a.[MilestoneVersion] = CASE WHEN m.[Version] > 0 THEN m.[Version] ELSE 1 END
                    FROM [Acceptances] a
                    INNER JOIN [ContractMilestones] m ON a.[MilestoneId] = m.[Id]
                    WHERE a.[MilestoneVersion] <= 0 OR a.[MilestoneVersion] IS NULL;

                    UPDATE [Acceptances] SET [MilestoneVersion] = 1 WHERE [MilestoneVersion] <= 0 OR [MilestoneVersion] IS NULL;

                    -- 3. Đảm bảo mọi Acceptance có Revision >= 1
                    UPDATE [Acceptances] SET [Revision] = 1 WHERE [Revision] <= 0 OR [Revision] IS NULL;

                    -- 4. Policy xử lý Acceptance legacy chưa hoàn tất:
                    -- Các biên bản đang ở trạng thái TechnicalApproved (3) nhưng thiếu tài liệu snapshot hoặc mã băm:
                    -- Đánh dấu ReviewInvalidated (Status = 4) yêu cầu Cán bộ kỹ thuật kiểm tra và lập biên bản mới trên mốc thực tế,
                    -- bảo đảm tính toàn vẹn trung thực của dữ liệu lịch sử, không bịa đặt hash/chữ ký.
                    UPDATE [Acceptances]
                    SET [Status] = 4,
                        [IsTechnicalPassed] = 0,
                        [Note] = CASE
                            WHEN [Note] IS NULL OR [Note] = '' THEN N'[HỆ THỐNG YÊU CẦU THẨM TRA LẠI] Biên bản kiểm tra kỹ thuật phiên bản cũ thiếu tài liệu snapshot toàn vẹn. Cần Cán bộ kỹ thuật kiểm tra và lập biên bản mới.'
                            ELSE [Note] + N' | [HỆ THỐNG YÊU CẦU THẨM TRA LẠI] Biên bản kiểm tra kỹ thuật phiên bản cũ thiếu tài liệu snapshot toàn vẹn. Cần Cán bộ kỹ thuật kiểm tra và lập biên bản mới.'
                        END
                    WHERE [Status] = 3 AND ([SnapshotDocumentHash] IS NULL OR [SnapshotDocumentHash] = '');
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [ProcuringEntityId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN
    CREATE INDEX [IX_BidPackages_ProcuringEntityId] ON [BidPackages] ([ProcuringEntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN
    ALTER TABLE [BidPackages] ADD CONSTRAINT [FK_BidPackages_ProcuringEntities_ProcuringEntityId] FOREIGN KEY ([ProcuringEntityId]) REFERENCES [ProcuringEntities] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN

                    -- 1. Backfill từ thẩm quyền ký (SigningAuthority.ProcuringEntityId) đã áp dụng trên nghiệm thu của hợp đồng thuộc gói thầu (nếu duy nhất 1 đơn vị)
                    ;WITH DeterministicFromAuthority AS (
                        SELECT c.[BidPackageId], MIN(sa.[ProcuringEntityId]) AS [ProcuringEntityId]
                        FROM [Contracts] c
                        INNER JOIN [Acceptances] a ON a.[ContractId] = c.[Id]
                        INNER JOIN [SigningAuthorities] sa ON sa.[Id] = a.[SigningAuthorityId]
                        WHERE a.[Status] = 1 AND a.[ApprovedBy] = sa.[UserId]
                          AND a.[TechnicalReviewedBy] <> a.[ApprovedBy]
                          AND LEN(a.[SnapshotDocumentHash]) = 64 AND a.[SnapshotDocumentPath] IS NOT NULL
                          AND sa.[VerifiedAt] IS NOT NULL AND sa.[VerifiedAt] <= a.[ApprovedAt]
                          AND sa.[ValidFrom] <= a.[ApprovedAt] AND sa.[ValidTo] >= a.[ApprovedAt]
                        GROUP BY c.[BidPackageId]
                        HAVING COUNT(DISTINCT sa.[ProcuringEntityId]) = 1
                    )
                    UPDATE bp
                    SET bp.[ProcuringEntityId] = d.[ProcuringEntityId]
                    FROM [BidPackages] bp
                    INNER JOIN DeterministicFromAuthority d ON d.[BidPackageId] = bp.[Id]
                    WHERE bp.[ProcuringEntityId] IS NULL;

                    -- Ownership cannot be inferred from present-day owner/member rows.
                    -- Unproven packages remain NULL until an audited evidence review.
                    -- 4. Chuẩn hóa Revision không trùng lặp trên mỗi Milestone trước khi tạo Unique Index (MilestoneId, Revision)
                    ;WITH NumberedAcceptances AS (
                        SELECT [Id], [MilestoneId], [Revision],
                               ROW_NUMBER() OVER (PARTITION BY [MilestoneId], [Revision] ORDER BY [Id] ASC) AS [DupRank],
                               ROW_NUMBER() OVER (PARTITION BY [MilestoneId] ORDER BY [Revision] ASC, [Id] ASC) AS [SeqRev]
                        FROM [Acceptances]
                        WHERE [MilestoneId] IS NOT NULL
                    )
                    UPDATE a
                    SET a.[Revision] = n.[SeqRev]
                    FROM [Acceptances] a
                    INNER JOIN NumberedAcceptances n ON n.[Id] = a.[Id]
                    WHERE EXISTS (
                        SELECT 1 FROM NumberedAcceptances d
                        WHERE d.[MilestoneId] = a.[MilestoneId] AND d.[DupRank] > 1
                    );
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Acceptances_MilestoneId_Revision] ON [Acceptances] ([MilestoneId], [Revision]) WHERE [MilestoneId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029173000_AddBidPackageProcuringEntityOwnership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029173000_AddBidPackageProcuringEntityOwnership', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120000_QuarantineUnprovenPackageOwnership'
)
BEGIN
    ALTER TABLE [BidPackages] ADD [OwnershipNeedsReview] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120000_QuarantineUnprovenPackageOwnership'
)
BEGIN

                UPDATE bp SET [OwnershipNeedsReview] = 1
                FROM [BidPackages] bp
                WHERE bp.[ProcuringEntityId] IS NULL OR NOT EXISTS (
                    SELECT 1 FROM [Contracts] c
                    JOIN [Acceptances] a ON a.[ContractId] = c.[Id]
                    JOIN [SigningAuthorities] sa ON sa.[Id] = a.[SigningAuthorityId]
                    WHERE c.[BidPackageId] = bp.[Id] AND sa.[ProcuringEntityId] = bp.[ProcuringEntityId]
                      AND a.[Status] = 1 AND a.[ApprovedBy] = sa.[UserId]
                      AND a.[TechnicalReviewedBy] <> a.[ApprovedBy]
                      AND LEN(a.[SnapshotDocumentHash]) = 64 AND a.[SnapshotDocumentPath] IS NOT NULL
                      AND sa.[VerifiedAt] IS NOT NULL AND sa.[VerifiedAt] <= a.[ApprovedAt]
                      AND sa.[ValidFrom] <= a.[ApprovedAt] AND sa.[ValidTo] >= a.[ApprovedAt]
                      AND NOT EXISTS (
                          SELECT 1 FROM [Contracts] c2 JOIN [Acceptances] a2 ON a2.[ContractId] = c2.[Id]
                          JOIN [SigningAuthorities] sa2 ON sa2.[Id] = a2.[SigningAuthorityId]
                          WHERE c2.[BidPackageId] = bp.[Id] AND a2.[Status] = 1
                            AND sa2.[ProcuringEntityId] <> bp.[ProcuringEntityId]
                      )
                );
                INSERT INTO [AuditLogs] ([Action], [EntityType], [EntityId], [OldValues], [NewValues], [Timestamp])
                SELECT 'QUARANTINE_PACKAGE_OWNERSHIP', 'BidPackage', bp.[Id],
                       (SELECT bp.[ProcuringEntityId] AS [CandidateProcuringEntityId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                       N'{"OwnershipNeedsReview":true,"Reason":"Historical ownership requires evidence review; candidate preserved"}',
                       SYSUTCDATETIME()
                FROM [BidPackages] bp WHERE bp.[OwnershipNeedsReview] = 1;
            
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120000_QuarantineUnprovenPackageOwnership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030120000_QuarantineUnprovenPackageOwnership', N'8.0.12');
END;
GO

COMMIT;
GO



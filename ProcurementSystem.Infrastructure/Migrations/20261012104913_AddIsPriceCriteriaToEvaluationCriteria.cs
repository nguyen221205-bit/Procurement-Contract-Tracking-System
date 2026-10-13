using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPriceCriteriaToEvaluationCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. R2-3: Thêm cột IsPriceCriteria vào bảng EvaluationCriteria và migrate dữ liệu cũ
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[EvaluationCriteria]') AND name = 'IsPriceCriteria')
                BEGIN
                    ALTER TABLE [EvaluationCriteria] ADD [IsPriceCriteria] bit NOT NULL CONSTRAINT DF_EvaluationCriteria_IsPriceCriteria DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                EXEC(N'UPDATE [EvaluationCriteria]
                SET [IsPriceCriteria] = 1
                WHERE [IsPriceCriteria] = 0 AND (
                    [Name] LIKE ''%Giá%'' OR [Name] LIKE ''%giá%'' OR
                    [Name] LIKE ''%Tài chính%'' OR [Name] LIKE ''%tài chính%'' OR
                    [Name] LIKE ''%Chi phí%'' OR [Name] LIKE ''%chi phí%'' OR
                    [Name] LIKE ''%Price%'' OR [Name] LIKE ''%price%''
                );');
            ");

            // 2. Bảo đảm an toàn cho các cột trên Contractors đã thêm trước đây
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignDate')
                    ALTER TABLE [Contractors] ADD [DigitalSignDate] datetime2 NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignMessage')
                    ALTER TABLE [Contractors] ADD [DigitalSignMessage] nvarchar(500) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignerName')
                    ALTER TABLE [Contractors] ADD [DigitalSignerName] nvarchar(250) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'IsDigitalSigned')
                    ALTER TABLE [Contractors] ADD [IsDigitalSigned] bit NOT NULL CONSTRAINT DF_Contractors_IsDigitalSigned DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'VerificationStatus')
                    ALTER TABLE [Contractors] ADD [VerificationStatus] nvarchar(50) NOT NULL CONSTRAINT DF_Contractors_VerificationStatus DEFAULT N'Approved';
            ");

            // 3. Bảo đảm an toàn cho các cột trên BidSubmissions đã thêm trước đây
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[BidSubmissions]') AND name = 'BidPrice')
                    ALTER TABLE [BidSubmissions] ADD [BidPrice] decimal(18,2) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[BidSubmissions]') AND name = 'SelectionReason')
                    ALTER TABLE [BidSubmissions] ADD [SelectionReason] nvarchar(1000) NULL;
            ");

            // 4. Bảo đảm an toàn cho các bảng BidPackageEvaluators, EvaluatorProposals, ProcuringEntities
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidPackageEvaluators')
                BEGIN
                    CREATE TABLE [BidPackageEvaluators] (
                        [BidPackageId] int NOT NULL,
                        [EvaluatorId] int NOT NULL,
                        [AssignedAt] datetime2 NOT NULL,
                        [AssignedBy] int NULL,
                        CONSTRAINT [PK_BidPackageEvaluators] PRIMARY KEY CLUSTERED ([BidPackageId], [EvaluatorId]),
                        CONSTRAINT [FK_BidPackageEvaluators_BidPackages_BidPackageId] FOREIGN KEY ([BidPackageId]) REFERENCES [BidPackages]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_BidPackageEvaluators_Users_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Users]([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_BidPackageEvaluators_Users_EvaluatorId] FOREIGN KEY ([EvaluatorId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                    );
                    CREATE NONCLUSTERED INDEX [IX_BidPackageEvaluators_AssignedBy] ON [BidPackageEvaluators] ([AssignedBy]);
                    CREATE NONCLUSTERED INDEX [IX_BidPackageEvaluators_EvaluatorId] ON [BidPackageEvaluators] ([EvaluatorId]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluatorProposals')
                BEGIN
                    CREATE TABLE [EvaluatorProposals] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [ProposerUserId] int NOT NULL,
                        [FullName] nvarchar(100) NOT NULL,
                        [Email] nvarchar(150) NOT NULL,
                        [Phone] nvarchar(20) NULL,
                        [Specialization] nvarchar(200) NULL,
                        [Workplace] nvarchar(250) NULL,
                        [ExperienceYears] int NULL,
                        [CertificateName] nvarchar(200) NULL,
                        [CertificateFile] nvarchar(500) NULL,
                        [Notes] nvarchar(1000) NULL,
                        [Status] nvarchar(50) NOT NULL,
                        [AdminNotes] nvarchar(1000) NULL,
                        [ReviewedByUserId] int NULL,
                        [ReviewedAt] datetime2 NULL,
                        [CreatedUserId] int NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_EvaluatorProposals] PRIMARY KEY CLUSTERED ([Id]),
                        CONSTRAINT [FK_EvaluatorProposals_Users_CreatedUserId] FOREIGN KEY ([CreatedUserId]) REFERENCES [Users]([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_EvaluatorProposals_Users_ProposerUserId] FOREIGN KEY ([ProposerUserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_EvaluatorProposals_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
                    );
                    CREATE NONCLUSTERED INDEX [IX_EvaluatorProposals_CreatedUserId] ON [EvaluatorProposals] ([CreatedUserId]);
                    CREATE NONCLUSTERED INDEX [IX_EvaluatorProposals_Email] ON [EvaluatorProposals] ([Email]);
                    CREATE NONCLUSTERED INDEX [IX_EvaluatorProposals_ProposerUserId] ON [EvaluatorProposals] ([ProposerUserId]);
                    CREATE NONCLUSTERED INDEX [IX_EvaluatorProposals_ReviewedByUserId] ON [EvaluatorProposals] ([ReviewedByUserId]);
                    CREATE NONCLUSTERED INDEX [IX_EvaluatorProposals_Status] ON [EvaluatorProposals] ([Status]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcuringEntities')
                BEGIN
                    CREATE TABLE [ProcuringEntities] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [UserId] int NOT NULL,
                        [OrganizationName] nvarchar(250) NOT NULL,
                        [OrganizationType] nvarchar(100) NOT NULL,
                        [TaxCode] nvarchar(20) NOT NULL,
                        [BudgetCode] nvarchar(50) NULL,
                        [Address] nvarchar(500) NULL,
                        [RepresentativeName] nvarchar(100) NOT NULL,
                        [RepresentativeTitle] nvarchar(100) NOT NULL,
                        [RepresentativePhone] nvarchar(20) NULL,
                        [EstablishmentDecisionFile] nvarchar(500) NOT NULL,
                        [AppointmentDecisionFile] nvarchar(500) NULL,
                        [VerificationStatus] nvarchar(50) NOT NULL,
                        [IsDigitalSigned] bit NOT NULL,
                        [DigitalSignerName] nvarchar(250) NULL,
                        [DigitalSignDate] datetime2 NULL,
                        [DigitalSignMessage] nvarchar(500) NULL,
                        [AdminNotes] nvarchar(1000) NULL,
                        [ReviewedByUserId] int NULL,
                        [ReviewedAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_ProcuringEntities] PRIMARY KEY CLUSTERED ([Id]),
                        CONSTRAINT [FK_ProcuringEntities_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_ProcuringEntities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                    );
                    CREATE NONCLUSTERED INDEX [IX_ProcuringEntities_ReviewedByUserId] ON [ProcuringEntities] ([ReviewedByUserId]);
                    CREATE NONCLUSTERED INDEX [IX_ProcuringEntities_TaxCode] ON [ProcuringEntities] ([TaxCode]);
                    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProcuringEntities_UserId] ON [ProcuringEntities] ([UserId]);
                    CREATE NONCLUSTERED INDEX [IX_ProcuringEntities_VerificationStatus] ON [ProcuringEntities] ([VerificationStatus]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BidPackageEvaluators");

            migrationBuilder.DropTable(
                name: "EvaluatorProposals");

            migrationBuilder.DropTable(
                name: "ProcuringEntities");

            migrationBuilder.DropColumn(
                name: "IsPriceCriteria",
                table: "EvaluationCriteria");

            migrationBuilder.DropColumn(
                name: "DigitalSignDate",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "DigitalSignMessage",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "DigitalSignerName",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "IsDigitalSigned",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "BidPrice",
                table: "BidSubmissions");

            migrationBuilder.DropColumn(
                name: "SelectionReason",
                table: "BidSubmissions");
        }
    }
}

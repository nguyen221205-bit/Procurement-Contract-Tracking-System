using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransparencyControlsIntegrityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BidPackageEvaluators_Users_EvaluatorId')
                        ALTER TABLE [BidPackageEvaluators] DROP CONSTRAINT [FK_BidPackageEvaluators_Users_EvaluatorId];
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BidPackageEvaluators_Users_Evaluator')
                        ALTER TABLE [BidPackageEvaluators] DROP CONSTRAINT [FK_BidPackageEvaluators_Users_Evaluator];
                ");
            }

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ContractMilestones",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ContractMilestones",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "Acceptances",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MilestoneVersion",
                table: "Acceptances",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Acceptances",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Acceptances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SigningAuthorityId",
                table: "Acceptances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SnapshotAmount",
                table: "Acceptances",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotDocumentHash",
                table: "Acceptances",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotDocumentPath",
                table: "Acceptances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SnapshotDueDate",
                table: "Acceptances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotTitle",
                table: "Acceptances",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SigningAuthorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcuringEntityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AuthorityType = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecisionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegalBasis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RevokedByUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningAuthorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SigningAuthorities_ProcuringEntities_ProcuringEntityId",
                        column: x => x.ProcuringEntityId,
                        principalTable: "ProcuringEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SigningAuthorities_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAuthorities_Users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAuthorities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAuthorities_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acceptances_SigningAuthorityId",
                table: "Acceptances",
                column: "SigningAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_ProcuringEntityId_UserId_Status",
                table: "SigningAuthorities",
                columns: new[] { "ProcuringEntityId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_RequestedByUserId",
                table: "SigningAuthorities",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_RevokedByUserId",
                table: "SigningAuthorities",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_Status",
                table: "SigningAuthorities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_UserId",
                table: "SigningAuthorities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAuthorities_VerifiedByUserId",
                table: "SigningAuthorities",
                column: "VerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Acceptances_SigningAuthorities_SigningAuthorityId",
                table: "Acceptances",
                column: "SigningAuthorityId",
                principalTable: "SigningAuthorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BidPackageEvaluators_Users_EvaluatorId",
                table: "BidPackageEvaluators",
                column: "EvaluatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                UPDATE [ContractMilestones] SET [Version] = 1 WHERE [Version] = 0;
                UPDATE [Acceptances] SET [MilestoneVersion] = 1 WHERE [MilestoneVersion] = 0;
                UPDATE [Acceptances] SET [Revision] = 1 WHERE [Revision] = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acceptances_SigningAuthorities_SigningAuthorityId",
                table: "Acceptances");

            migrationBuilder.DropForeignKey(
                name: "FK_BidPackageEvaluators_Users_EvaluatorId",
                table: "BidPackageEvaluators");

            migrationBuilder.DropTable(
                name: "SigningAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_Acceptances_SigningAuthorityId",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ContractMilestones");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ContractMilestones");

            migrationBuilder.DropColumn(
                name: "MilestoneVersion",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SigningAuthorityId",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SnapshotAmount",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SnapshotDocumentHash",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SnapshotDocumentPath",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SnapshotDueDate",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "SnapshotTitle",
                table: "Acceptances");

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "Acceptances",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BidPackageEvaluators_Users_EvaluatorId",
                table: "BidPackageEvaluators",
                column: "EvaluatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

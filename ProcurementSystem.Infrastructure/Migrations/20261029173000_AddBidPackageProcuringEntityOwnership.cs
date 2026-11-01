using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBidPackageProcuringEntityOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcuringEntityId",
                table: "BidPackages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BidPackages_ProcuringEntityId",
                table: "BidPackages",
                column: "ProcuringEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BidPackages_ProcuringEntities_ProcuringEntityId",
                table: "BidPackages",
                column: "ProcuringEntityId",
                principalTable: "ProcuringEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // R4-01: Backfill ProcuringEntityId cho các gói thầu legacy CHỈ KHI xác định được nguồn gốc đơn vị chắc chắn (deterministic).
            // Các gói thầu có lịch sử người tạo thuộc nhiều đơn vị (ambiguous) được giữ nguyên ProcuringEntityId = NULL (fail-closed)
            // để buộc đối soát thay vì suy diễn theo membership hiện tại.
            migrationBuilder.Sql(@"
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
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Acceptances_MilestoneId_Revision",
                table: "Acceptances",
                columns: new[] { "MilestoneId", "Revision" },
                unique: true,
                filter: "[MilestoneId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Acceptances_MilestoneId_Revision",
                table: "Acceptances");

            migrationBuilder.DropForeignKey(
                name: "FK_BidPackages_ProcuringEntities_ProcuringEntityId",
                table: "BidPackages");

            migrationBuilder.DropIndex(
                name: "IX_BidPackages_ProcuringEntityId",
                table: "BidPackages");

            migrationBuilder.DropColumn(
                name: "ProcuringEntityId",
                table: "BidPackages");
        }
    }
}

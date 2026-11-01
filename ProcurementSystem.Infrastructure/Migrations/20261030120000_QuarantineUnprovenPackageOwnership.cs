using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations;

public partial class QuarantineUnprovenPackageOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "OwnershipNeedsReview", table: "BidPackages", type: "bit", nullable: false, defaultValue: false);
        // The old migration did not record which inference populated each row.
        // Preserve candidates, but quarantine every existing row lacking a linked,
        // verified historical approval. Present-day memberships are not provenance.
        migrationBuilder.Sql(@"
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
                   N'{""OwnershipNeedsReview"":true,""Reason"":""Historical ownership requires evidence review; candidate preserved""}',
                   SYSUTCDATETIME()
            FROM [BidPackages] bp WHERE bp.[OwnershipNeedsReview] = 1;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Downgrading removes enforcement; retained audit records document candidates.
        migrationBuilder.DropColumn(name: "OwnershipNeedsReview", table: "BidPackages");
    }
}

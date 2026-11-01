using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // T03 / T02: Thêm cột RowVersion cho ProcuringEntityMembers để kiểm soát đồng thời lạc quan (optimistic concurrency)
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcuringEntityMembers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // T03: Forward Repair & Backfill cho cả DB nâng cấp từ bản cũ (đã chạy 20261028094453) lẫn DB mới
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcuringEntityMembers");
        }
    }
}

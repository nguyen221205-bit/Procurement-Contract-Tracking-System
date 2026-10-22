using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClarificationsAndApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BidClarifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BidPackageId = table.Column<int>(type: "int", nullable: false),
                    BidSubmissionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    QuestionContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    QuestionAttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AskedByUserId = table.Column<int>(type: "int", nullable: false),
                    AskedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnswerContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    AnswerAttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnsweredByUserId = table.Column<int>(type: "int", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidClarifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BidClarifications_BidPackages_BidPackageId",
                        column: x => x.BidPackageId,
                        principalTable: "BidPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BidClarifications_BidSubmissions_BidSubmissionId",
                        column: x => x.BidSubmissionId,
                        principalTable: "BidSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BidClarifications_Users_AnsweredByUserId",
                        column: x => x.AnsweredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BidClarifications_Users_AskedByUserId",
                        column: x => x.AskedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageStatusApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BidPackageId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    TargetStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NewDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageStatusApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageStatusApprovalRequests_BidPackages_BidPackageId",
                        column: x => x.BidPackageId,
                        principalTable: "BidPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageStatusApprovalRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageStatusApprovalRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BidClarifications_AnsweredByUserId",
                table: "BidClarifications",
                column: "AnsweredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BidClarifications_AskedByUserId",
                table: "BidClarifications",
                column: "AskedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BidClarifications_BidPackageId",
                table: "BidClarifications",
                column: "BidPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_BidClarifications_BidSubmissionId",
                table: "BidClarifications",
                column: "BidSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_BidClarifications_Type",
                table: "BidClarifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PackageStatusApprovalRequests_BidPackageId",
                table: "PackageStatusApprovalRequests",
                column: "BidPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageStatusApprovalRequests_RequestedByUserId",
                table: "PackageStatusApprovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageStatusApprovalRequests_ReviewedByUserId",
                table: "PackageStatusApprovalRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageStatusApprovalRequests_Status",
                table: "PackageStatusApprovalRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BidClarifications");

            migrationBuilder.DropTable(
                name: "PackageStatusApprovalRequests");
        }
    }
}

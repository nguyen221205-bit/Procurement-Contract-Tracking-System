using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabasePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_ContractorId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractMilestones_ContractId",
                table: "ContractMilestones");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractorId_Status",
                table: "Contracts",
                columns: new[] { "ContractorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractMilestones_ContractId_Status",
                table: "ContractMilestones",
                columns: new[] { "ContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BidSubmissions_BidPackageId_Status",
                table: "BidSubmissions",
                columns: new[] { "BidPackageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BidSubmissions_TotalScore",
                table: "BidSubmissions",
                column: "TotalScore");

            migrationBuilder.CreateIndex(
                name: "IX_BidPackages_Status_Deadline",
                table: "BidPackages",
                columns: new[] { "Status", "Deadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_ContractorId_Status",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractMilestones_ContractId_Status",
                table: "ContractMilestones");

            migrationBuilder.DropIndex(
                name: "IX_BidSubmissions_BidPackageId_Status",
                table: "BidSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BidSubmissions_TotalScore",
                table: "BidSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_BidPackages_Status_Deadline",
                table: "BidPackages");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractorId",
                table: "Contracts",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMilestones_ContractId",
                table: "ContractMilestones",
                column: "ContractId");
        }
    }
}

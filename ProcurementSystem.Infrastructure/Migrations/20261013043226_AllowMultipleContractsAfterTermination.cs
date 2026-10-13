using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleContractsAfterTermination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_BidPackageId",
                table: "Contracts");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BidPackageId",
                table: "Contracts",
                column: "BidPackageId",
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_BidPackageId",
                table: "Contracts");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BidPackageId",
                table: "Contracts",
                column: "BidPackageId",
                unique: true);
        }
    }
}

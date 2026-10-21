using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractGuaranteesAddendaEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractAddenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    AddendumNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AdditionalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtensionDays = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScannedDocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAddenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAddenda_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractAddenda_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractAddenda_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractGuarantees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    GuaranteeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScannedDocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractGuarantees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractGuarantees_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractorEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ContractorId = table.Column<int>(type: "int", nullable: false),
                    EvaluatorUserId = table.Column<int>(type: "int", nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ScheduleScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RatingGrade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractorEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractorEvaluations_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractorEvaluations_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractorEvaluations_Users_EvaluatorUserId",
                        column: x => x.EvaluatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddenda_AddendumNumber",
                table: "ContractAddenda",
                column: "AddendumNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddenda_ApprovedBy",
                table: "ContractAddenda",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddenda_ContractId",
                table: "ContractAddenda",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddenda_CreatedBy",
                table: "ContractAddenda",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContractGuarantees_ContractId",
                table: "ContractGuarantees",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractGuarantees_ExpiryDate",
                table: "ContractGuarantees",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractGuarantees_GuaranteeNumber",
                table: "ContractGuarantees",
                column: "GuaranteeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractGuarantees_Status",
                table: "ContractGuarantees",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorEvaluations_ContractId_ContractorId",
                table: "ContractorEvaluations",
                columns: new[] { "ContractId", "ContractorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractorEvaluations_ContractorId",
                table: "ContractorEvaluations",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorEvaluations_EvaluatorUserId",
                table: "ContractorEvaluations",
                column: "EvaluatorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractAddenda");

            migrationBuilder.DropTable(
                name: "ContractGuarantees");

            migrationBuilder.DropTable(
                name: "ContractorEvaluations");
        }
    }
}

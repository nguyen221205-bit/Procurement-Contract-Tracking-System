using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcuringEntityMembershipAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTechnicalPassed",
                table: "Acceptances",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalNote",
                table: "Acceptances",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TechnicalReviewedAt",
                table: "Acceptances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalReviewedBy",
                table: "Acceptances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcuringEntityInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcuringEntityId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RoleInEntity = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InvitedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcuringEntityInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityInvitations_ProcuringEntities_ProcuringEntityId",
                        column: x => x.ProcuringEntityId,
                        principalTable: "ProcuringEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityInvitations_Users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcuringEntityMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcuringEntityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleInEntity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcuringEntityMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityMembers_ProcuringEntities_ProcuringEntityId",
                        column: x => x.ProcuringEntityId,
                        principalTable: "ProcuringEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityMembers_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityMembers_Users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcuringEntityMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acceptances_TechnicalReviewedBy",
                table: "Acceptances",
                column: "TechnicalReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_AcceptedByUserId",
                table: "ProcuringEntityInvitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_Email",
                table: "ProcuringEntityInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_ExpiresAt",
                table: "ProcuringEntityInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_InvitedByUserId",
                table: "ProcuringEntityInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_ProcuringEntityId",
                table: "ProcuringEntityInvitations",
                column: "ProcuringEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_Status",
                table: "ProcuringEntityInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityInvitations_TokenHash",
                table: "ProcuringEntityInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_InvitedByUserId",
                table: "ProcuringEntityMembers",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_ProcuringEntityId_UserId",
                table: "ProcuringEntityMembers",
                columns: new[] { "ProcuringEntityId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_RevokedByUserId",
                table: "ProcuringEntityMembers",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_RoleInEntity",
                table: "ProcuringEntityMembers",
                column: "RoleInEntity");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_Status",
                table: "ProcuringEntityMembers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcuringEntityMembers_UserId",
                table: "ProcuringEntityMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Acceptances_Users_TechnicalReviewedBy",
                table: "Acceptances",
                column: "TechnicalReviewedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO ProcuringEntityMembers (ProcuringEntityId, UserId, RoleInEntity, Status, JoinedAt)
                SELECT pe.Id, pe.UserId, 0, 0, pe.CreatedAt
                FROM ProcuringEntities pe
                WHERE NOT EXISTS (
                    SELECT 1 FROM ProcuringEntityMembers pem
                    WHERE pem.ProcuringEntityId = pe.Id AND pem.UserId = pe.UserId
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acceptances_Users_TechnicalReviewedBy",
                table: "Acceptances");

            migrationBuilder.DropTable(
                name: "ProcuringEntityInvitations");

            migrationBuilder.DropTable(
                name: "ProcuringEntityMembers");

            migrationBuilder.DropIndex(
                name: "IX_Acceptances_TechnicalReviewedBy",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "IsTechnicalPassed",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "TechnicalNote",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewedAt",
                table: "Acceptances");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewedBy",
                table: "Acceptances");
        }
    }
}

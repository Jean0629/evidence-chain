using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvidenceChain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyTransfers_EvidenceId",
                table: "CustodyTransfers",
                column: "EvidenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustodyTransfers_Evidences_EvidenceId",
                table: "CustodyTransfers",
                column: "EvidenceId",
                principalTable: "Evidences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustodyTransfers_Evidences_EvidenceId",
                table: "CustodyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences");

            migrationBuilder.DropIndex(
                name: "IX_CustodyTransfers_EvidenceId",
                table: "CustodyTransfers");
        }
    }
}

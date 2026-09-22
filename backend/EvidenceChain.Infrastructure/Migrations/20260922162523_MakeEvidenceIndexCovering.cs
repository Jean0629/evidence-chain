using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvidenceChain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeEvidenceIndexCovering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences",
                columns: new[] { "CreatedAtUtc", "Id" })
                .Annotation("SqlServer:Include", new[] { "CurrentCustodianId", "Description", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CreatedAtUtc_Id",
                table: "Evidences",
                columns: new[] { "CreatedAtUtc", "Id" });
        }
    }
}

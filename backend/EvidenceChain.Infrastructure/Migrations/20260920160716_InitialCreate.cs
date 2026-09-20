using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvidenceChain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Custodians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    LoginCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyKeys",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKeys", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "CustodyTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCustodianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCustodianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyTransfers_Custodians_FromCustodianId",
                        column: x => x.FromCustodianId,
                        principalTable: "Custodians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyTransfers_Custodians_ToCustodianId",
                        column: x => x.ToCustodianId,
                        principalTable: "Custodians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentCustodianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evidences_Custodians_CurrentCustodianId",
                        column: x => x.CurrentCustodianId,
                        principalTable: "Custodians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyEvents_Custodians_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Custodians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvents_CustodyTransfers_RelatedTransferId",
                        column: x => x.RelatedTransferId,
                        principalTable: "CustodyTransfers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustodyEvents_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Custodians_LoginCode",
                table: "Custodians",
                column: "LoginCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvents_ActorId",
                table: "CustodyEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvents_EvidenceId",
                table: "CustodyEvents",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvents_RelatedTransferId",
                table: "CustodyEvents",
                column: "RelatedTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyTransfers_FromCustodianId",
                table: "CustodyTransfers",
                column: "FromCustodianId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyTransfers_ToCustodianId",
                table: "CustodyTransfers",
                column: "ToCustodianId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CurrentCustodianId_CreatedAtUtc",
                table: "Evidences",
                columns: new[] { "CurrentCustodianId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustodyEvents");

            migrationBuilder.DropTable(
                name: "IdempotencyKeys");

            migrationBuilder.DropTable(
                name: "CustodyTransfers");

            migrationBuilder.DropTable(
                name: "Evidences");

            migrationBuilder.DropTable(
                name: "Custodians");
        }
    }
}

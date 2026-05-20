using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseGovernance.Adapters.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditResults",
                columns: table => new
                {
                    ScanDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditResults", x => x.ScanDateTime);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Group = table.Column<string>(type: "TEXT", nullable: false),
                    IsActiveInTenant = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantAuditResultScanDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypes_AuditResults_TenantAuditResultScanDateTime",
                        column: x => x.TenantAuditResultScanDateTime,
                        principalTable: "AuditResults",
                        principalColumn: "ScanDateTime",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InternalName = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSealed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentTypeDefinitionId = table.Column<string>(type: "TEXT", nullable: true),
                    TenantAuditResultScanDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fields_AuditResults_TenantAuditResultScanDateTime",
                        column: x => x.TenantAuditResultScanDateTime,
                        principalTable: "AuditResults",
                        principalColumn: "ScanDateTime",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fields_ContentTypes_ContentTypeDefinitionId",
                        column: x => x.ContentTypeDefinitionId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_TenantAuditResultScanDateTime",
                table: "ContentTypes",
                column: "TenantAuditResultScanDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ContentTypeDefinitionId",
                table: "Fields",
                column: "ContentTypeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_TenantAuditResultScanDateTime",
                table: "Fields",
                column: "TenantAuditResultScanDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fields");

            migrationBuilder.DropTable(
                name: "ContentTypes");

            migrationBuilder.DropTable(
                name: "AuditResults");
        }
    }
}

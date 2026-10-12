using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVatRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VatRule",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxCategoryCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRule", x => x.Id);
                    table.CheckConstraint("CK_VatRule_Rate", "\"Rate\" >= 0 AND \"Rate\" < 1");
                    table.ForeignKey(
                        name: "FK_VatRule_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatRule_MarketId",
                schema: "public",
                table: "VatRule",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "UX_VatRule_TenantId_MarketId_TaxCategoryCode",
                schema: "public",
                table: "VatRule",
                columns: new[] { "TenantId", "MarketId", "TaxCategoryCode" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VatRule",
                schema: "public");
        }
    }
}

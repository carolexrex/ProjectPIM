using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryLocation",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalance",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IncomingQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Backorderable = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBalance_InventoryLocation_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "public",
                        principalTable: "InventoryLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryBalance_Variant_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "public",
                        principalTable: "Variant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLocationMarketAssignment",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLocationMarketAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLocationMarketAssignment_InventoryLocation_Invento~",
                        column: x => x.InventoryLocationId,
                        principalSchema: "public",
                        principalTable: "InventoryLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryLocationMarketAssignment_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransaction",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_InventoryLocation_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "public",
                        principalTable: "InventoryLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_Variant_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "public",
                        principalTable: "Variant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_InventoryLocationId_VariantId",
                schema: "public",
                table: "InventoryBalance",
                columns: new[] { "InventoryLocationId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_VariantId",
                schema: "public",
                table: "InventoryBalance",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocation_TenantId_Code",
                schema: "public",
                table: "InventoryLocation",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocationMarketAssignment_InventoryLocationId_Marke~",
                schema: "public",
                table: "InventoryLocationMarketAssignment",
                columns: new[] { "InventoryLocationId", "MarketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocationMarketAssignment_MarketId",
                schema: "public",
                table: "InventoryLocationMarketAssignment",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_InventoryLocationId_OccurredAtUtc",
                schema: "public",
                table: "InventoryTransaction",
                columns: new[] { "InventoryLocationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_VariantId_OccurredAtUtc",
                schema: "public",
                table: "InventoryTransaction",
                columns: new[] { "VariantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBalance",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InventoryLocationMarketAssignment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InventoryTransaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InventoryLocation",
                schema: "public");
        }
    }
}

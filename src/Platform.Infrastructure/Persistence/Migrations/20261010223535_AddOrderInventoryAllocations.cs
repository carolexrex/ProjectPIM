using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInventoryAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReservedQuantityDelta",
                schema: "public",
                table: "InventoryTransaction",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "InventoryAllocation",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryBalanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BackorderedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAllocation", x => x.Id);
                    table.CheckConstraint("CK_InventoryAllocation_BackorderedQuantity", "\"BackorderedQuantity\" >= 0 AND \"BackorderedQuantity\" <= \"Quantity\"");
                    table.CheckConstraint("CK_InventoryAllocation_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_InventoryAllocation_InventoryBalance_InventoryBalanceId",
                        column: x => x.InventoryBalanceId,
                        principalSchema: "public",
                        principalTable: "InventoryBalance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAllocation_InventoryLocation_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "public",
                        principalTable: "InventoryLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAllocation_Order_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAllocation_InventoryBalanceId_Status",
                schema: "public",
                table: "InventoryAllocation",
                columns: new[] { "InventoryBalanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAllocation_InventoryLocationId",
                schema: "public",
                table: "InventoryAllocation",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAllocation_VariantId",
                schema: "public",
                table: "InventoryAllocation",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryAllocation_OrderId_InventoryBalanceId",
                schema: "public",
                table: "InventoryAllocation",
                columns: new[] { "OrderId", "InventoryBalanceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAllocation",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "ReservedQuantityDelta",
                schema: "public",
                table: "InventoryTransaction");
        }
    }
}

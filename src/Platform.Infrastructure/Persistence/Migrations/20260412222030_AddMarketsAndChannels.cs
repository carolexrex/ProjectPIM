using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketsAndChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channel",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HostName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Market",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DefaultCulture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    VatMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Market", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMarketAssignment",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMarketAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMarketAssignment_Channel_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "public",
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelMarketAssignment_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketCulture",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CultureCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketCulture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketCulture_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketCurrency",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketCurrency", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketCurrency_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketProductAssignment",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketProductAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketProductAssignment_Market_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "public",
                        principalTable: "Market",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketProductAssignment_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "public",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Channel_TenantId_Code",
                schema: "public",
                table: "Channel",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMarketAssignment_ChannelId_MarketId",
                schema: "public",
                table: "ChannelMarketAssignment",
                columns: new[] { "ChannelId", "MarketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMarketAssignment_MarketId",
                schema: "public",
                table: "ChannelMarketAssignment",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Market_TenantId_Code",
                schema: "public",
                table: "Market",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketCulture_MarketId_CultureCode",
                schema: "public",
                table: "MarketCulture",
                columns: new[] { "MarketId", "CultureCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketCurrency_MarketId_CurrencyCode",
                schema: "public",
                table: "MarketCurrency",
                columns: new[] { "MarketId", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketProductAssignment_MarketId_ProductId",
                schema: "public",
                table: "MarketProductAssignment",
                columns: new[] { "MarketId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketProductAssignment_ProductId",
                schema: "public",
                table: "MarketProductAssignment",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMarketAssignment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MarketCulture",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MarketCurrency",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MarketProductAssignment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Channel",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Market",
                schema: "public");
        }
    }
}

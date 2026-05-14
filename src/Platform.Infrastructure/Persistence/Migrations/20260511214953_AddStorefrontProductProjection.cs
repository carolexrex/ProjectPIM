using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStorefrontProductProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorefrontProductProjection",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CultureCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ProductNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProductType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LongDescription = table.Column<string>(type: "text", nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrandCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrandName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BrandSlug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BrandWebsiteUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    BrandLogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CategoryCodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CategorySlugsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CategoryNamesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CategoryFilterSlugsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CategoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    PrimaryImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    AttributesJson = table.Column<string>(type: "jsonb", nullable: false),
                    MediaJson = table.Column<string>(type: "jsonb", nullable: false),
                    HasVariants = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuyable = table.Column<bool>(type: "boolean", nullable: false),
                    BuyabilityReasonsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IsBackorderable = table.Column<bool>(type: "boolean", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CompareAtAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    VatIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    PriceListCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VariantsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SearchText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    SortName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SortProductNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    BrandSortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectionVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontProductProjection", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_AvailabilityStatus",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "AvailabilityStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_BrandCode",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "BrandCode");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_IsBuyable",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "IsBuyable");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_IsVisible",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "IsVisible");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_MarketCode_CultureCode_Currenc~1",
                schema: "public",
                table: "StorefrontProductProjection",
                columns: new[] { "MarketCode", "CultureCode", "CurrencyCode", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_MarketCode_CultureCode_Currency~",
                schema: "public",
                table: "StorefrontProductProjection",
                columns: new[] { "MarketCode", "CultureCode", "CurrencyCode", "ProductNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_ProductId",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_SortName",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "SortName");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductProjection_SortPriceAmount",
                schema: "public",
                table: "StorefrontProductProjection",
                column: "SortPriceAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontProductProjection",
                schema: "public");
        }
    }
}

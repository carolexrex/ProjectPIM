using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PlatformDbContext))]
    [Migration("20260411003912_SeedCatalogStatuses")]
    public partial class SeedCatalogStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createdAtUtc = new DateTime(2026, 4, 11, 0, 39, 12, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                schema: "public",
                table: "ProductStatusDefinition",
                columns: new[]
                {
                    "Id",
                    "EntityType",
                    "Code",
                    "Name",
                    "IsBuyable",
                    "CreatedAtUtc",
                    "IsDefault",
                    "IsSearchable",
                    "IsVisibleInBackoffice",
                    "IsVisibleInStorefront",
                    "RowVersion",
                    "SortOrder",
                    "Status",
                    "TenantId",
                    "UpdatedAtUtc"
                },
                columnTypes: new[]
                {
                    "uuid",
                    "character varying(32)",
                    "character varying(64)",
                    "character varying(128)",
                    "boolean",
                    "timestamp with time zone",
                    "boolean",
                    "boolean",
                    "boolean",
                    "boolean",
                    "character varying(64)",
                    "integer",
                    "character varying(32)",
                    "uuid",
                    "timestamp with time zone"
                },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Product", "DRAFT", "Draft", false, createdAtUtc, false, true, true, true, "seed-product-draft-v1", 10, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Product", "READY", "Ready", true, createdAtUtc, false, true, true, true, "seed-product-ready-v1", 20, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Product", "COMING_SOON", "Coming Soon", false, createdAtUtc, false, true, true, true, "seed-product-coming-soon-v1", 30, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Product", "DISCONTINUED", "Discontinued", false, createdAtUtc, false, true, true, true, "seed-product-discontinued-v1", 40, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000101"), "Variant", "DRAFT", "Draft", false, createdAtUtc, false, true, true, true, "seed-variant-draft-v1", 10, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000102"), "Variant", "READY", "Ready", true, createdAtUtc, false, true, true, true, "seed-variant-ready-v1", 20, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000103"), "Variant", "COMING_SOON", "Coming Soon", false, createdAtUtc, false, true, true, true, "seed-variant-coming-soon-v1", 30, "Active", null, createdAtUtc },
                    { new Guid("10000000-0000-0000-0000-000000000104"), "Variant", "DISCONTINUED", "Discontinued", false, createdAtUtc, false, true, true, true, "seed-variant-discontinued-v1", 40, "Active", null, createdAtUtc }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "ProductStatusDefinition",
                keyColumns: new[] { "Id" },
                keyValues: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("10000000-0000-0000-0000-000000000101") },
                    { new Guid("10000000-0000-0000-0000-000000000102") },
                    { new Guid("10000000-0000-0000-0000-000000000103") },
                    { new Guid("10000000-0000-0000-0000-000000000104") }
                });
        }
    }
}

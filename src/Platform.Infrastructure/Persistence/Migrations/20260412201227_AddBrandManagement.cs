using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brand",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LogoMediaAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Brand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brand_MediaAsset_LogoMediaAssetId",
                        column: x => x.LogoMediaAssetId,
                        principalSchema: "public",
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BrandTranslation",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CultureCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandTranslation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandTranslation_Brand_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "public",
                        principalTable: "Brand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Product_BrandId",
                schema: "public",
                table: "Product",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_LogoMediaAssetId",
                schema: "public",
                table: "Brand",
                column: "LogoMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_TenantId_Code",
                schema: "public",
                table: "Brand",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandTranslation_BrandId_CultureCode",
                schema: "public",
                table: "BrandTranslation",
                columns: new[] { "BrandId", "CultureCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Brand_BrandId",
                schema: "public",
                table: "Product",
                column: "BrandId",
                principalSchema: "public",
                principalTable: "Brand",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_Brand_BrandId",
                schema: "public",
                table: "Product");

            migrationBuilder.DropTable(
                name: "BrandTranslation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Brand",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Product_BrandId",
                schema: "public",
                table: "Product");
        }
    }
}

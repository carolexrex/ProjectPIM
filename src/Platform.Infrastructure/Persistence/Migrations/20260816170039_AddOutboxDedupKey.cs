using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDedupKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DedupKey",
                schema: "public",
                table: "OutboxMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_DedupKey_Open",
                schema: "public",
                table: "OutboxMessage",
                column: "DedupKey",
                unique: true,
                filter: "\"DedupKey\" IS NOT NULL AND \"PublishedAtUtc\" IS NULL AND \"ProcessingAbandonedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_DedupKey_Open",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "DedupKey",
                schema: "public",
                table: "OutboxMessage");
        }
    }
}

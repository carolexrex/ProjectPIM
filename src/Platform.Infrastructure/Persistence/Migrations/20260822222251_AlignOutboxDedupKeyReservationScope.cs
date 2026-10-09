using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignOutboxDedupKeyReservationScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_DedupKey_Open",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_DedupKey_Unpublished",
                schema: "public",
                table: "OutboxMessage",
                column: "DedupKey",
                unique: true,
                filter: "\"DedupKey\" IS NOT NULL AND \"PublishedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_DedupKey_Unpublished",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_DedupKey_Open",
                schema: "public",
                table: "OutboxMessage",
                column: "DedupKey",
                unique: true,
                filter: "\"DedupKey\" IS NOT NULL AND \"PublishedAtUtc\" IS NULL AND \"ProcessingAbandonedAtUtc\" IS NULL");
        }
    }
}

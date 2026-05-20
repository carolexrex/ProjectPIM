using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxProcessingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastProcessingError",
                schema: "public",
                table: "OutboxMessage",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextProcessingAttemptAtUtc",
                schema: "public",
                table: "OutboxMessage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingAbandonedAtUtc",
                schema: "public",
                table: "OutboxMessage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingAttemptCount",
                schema: "public",
                table: "OutboxMessage",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_NextProcessingAttemptAtUtc",
                schema: "public",
                table: "OutboxMessage",
                column: "NextProcessingAttemptAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessingAbandonedAtUtc",
                schema: "public",
                table: "OutboxMessage",
                column: "ProcessingAbandonedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_NextProcessingAttemptAtUtc",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessingAbandonedAtUtc",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "LastProcessingError",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "NextProcessingAttemptAtUtc",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "ProcessingAbandonedAtUtc",
                schema: "public",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "ProcessingAttemptCount",
                schema: "public",
                table: "OutboxMessage");
        }
    }
}

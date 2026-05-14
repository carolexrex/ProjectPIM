using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationJobsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationJob",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResultPayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResultSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationJob", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJob_CreatedAtUtc",
                schema: "public",
                table: "IntegrationJob",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJob_NextAttemptAtUtc",
                schema: "public",
                table: "IntegrationJob",
                column: "NextAttemptAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJob_Status",
                schema: "public",
                table: "IntegrationJob",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJob_Type",
                schema: "public",
                table: "IntegrationJob",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationJob",
                schema: "public");
        }
    }
}

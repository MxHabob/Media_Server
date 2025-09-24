using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionConfiguration_Recreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubscriptionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomDurationHours = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxConcurrentSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowRemoteAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowTranscoding = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParentalRating = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSyncPlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_CreatedDate",
                table: "SubscriptionConfigurations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_IsActive",
                table: "SubscriptionConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_SubscriptionType",
                table: "SubscriptionConfigurations",
                column: "SubscriptionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionConfigurations");
        }
    }
}

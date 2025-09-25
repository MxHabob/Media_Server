using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MakePinBatchUserUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_PinBatchUsers_Users_UserId",
                table: "PinBatchUsers");

            // Make the UserId column nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PinBatchUsers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            // Recreate the foreign key constraint with nullable UserId
            migrationBuilder.AddForeignKey(
                name: "FK_PinBatchUsers_Users_UserId",
                table: "PinBatchUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the nullable foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_PinBatchUsers_Users_UserId",
                table: "PinBatchUsers");

            // Make the UserId column non-nullable again
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PinBatchUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            // Recreate the original foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_PinBatchUsers_Users_UserId",
                table: "PinBatchUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

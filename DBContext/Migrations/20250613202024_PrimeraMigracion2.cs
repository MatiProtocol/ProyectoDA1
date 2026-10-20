using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBContext.Migrations
{
    /// <inheritdoc />
    public partial class PrimeraMigracion2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Users_UserToNotifyId",
                table: "Notification");

            migrationBuilder.RenameColumn(
                name: "UserToNotifyId",
                table: "Notification",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_UserToNotifyId",
                table: "Notification",
                newName: "IX_Notification_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Notification",
                newName: "UserToNotifyId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                newName: "IX_Notification_UserToNotifyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Users_UserToNotifyId",
                table: "Notification",
                column: "UserToNotifyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

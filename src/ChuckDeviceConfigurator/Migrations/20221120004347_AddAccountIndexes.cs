using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceConfigurator.Migrations
{
    public partial class AddAccountIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "group",
                table: "account",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "failed",
                table: "account",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_account_failed",
                table: "account",
                column: "failed");

            migrationBuilder.CreateIndex(
                name: "IX_account_group",
                table: "account",
                column: "group");

            migrationBuilder.CreateIndex(
                name: "IX_account_level",
                table: "account",
                column: "level");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_failed",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_group",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_level",
                table: "account");

            migrationBuilder.AlterColumn<string>(
                name: "group",
                table: "account",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "failed",
                table: "account",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

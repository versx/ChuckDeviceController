using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceConfigurator.Migrations
{
    public partial class AddMoreAccountIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_account_failed_timestamp",
                table: "account",
                column: "failed_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_account_first_warning_timestamp",
                table: "account",
                column: "first_warning_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_account_warn",
                table: "account",
                column: "warn");

            migrationBuilder.CreateIndex(
                name: "IX_account_warn_expire_timestamp",
                table: "account",
                column: "warn_expire_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_account_was_suspended",
                table: "account",
                column: "was_suspended");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_failed_timestamp",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_first_warning_timestamp",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_warn",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_warn_expire_timestamp",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_was_suspended",
                table: "account");
        }
    }
}

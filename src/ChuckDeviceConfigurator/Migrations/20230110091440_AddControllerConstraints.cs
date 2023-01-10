using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceConfigurator.Migrations
{
    /// <inheritdoc />
    public partial class AddControllerConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_account_account_username",
                table: "device");

            migrationBuilder.DropIndex(
                name: "IX_device_account_username",
                table: "device");

            migrationBuilder.CreateIndex(
                name: "IX_device_account_username",
                table: "device",
                column: "account_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_last_encounter_lat_last_encounter_lon",
                table: "account",
                columns: new[] { "last_encounter_lat", "last_encounter_lon" });

            migrationBuilder.CreateIndex(
                name: "IX_account_last_encounter_time",
                table: "account",
                column: "last_encounter_time");

            migrationBuilder.CreateIndex(
                name: "IX_account_last_used_timestamp",
                table: "account",
                column: "last_used_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_account_spins",
                table: "account",
                column: "spins");

            migrationBuilder.AddForeignKey(
                name: "FK_device_account_account_username",
                table: "device",
                column: "account_username",
                principalTable: "account",
                principalColumn: "username",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_account_account_username",
                table: "device");

            migrationBuilder.DropIndex(
                name: "IX_device_account_username",
                table: "device");

            migrationBuilder.DropIndex(
                name: "IX_account_last_encounter_lat_last_encounter_lon",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_last_encounter_time",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_last_used_timestamp",
                table: "account");

            migrationBuilder.DropIndex(
                name: "IX_account_spins",
                table: "account");

            migrationBuilder.CreateIndex(
                name: "IX_device_account_username",
                table: "device",
                column: "account_username");

            migrationBuilder.AddForeignKey(
                name: "FK_device_account_account_username",
                table: "device",
                column: "account_username",
                principalTable: "account",
                principalColumn: "username");
        }
    }
}

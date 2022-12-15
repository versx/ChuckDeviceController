using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class AddS2CellSpawnpointIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_spawnpoint_despawn_sec",
                table: "spawnpoint",
                column: "despawn_sec");

            migrationBuilder.CreateIndex(
                name: "IX_spawnpoint_lat",
                table: "spawnpoint",
                column: "lat");

            migrationBuilder.CreateIndex(
                name: "IX_spawnpoint_lon",
                table: "spawnpoint",
                column: "lon");

            migrationBuilder.CreateIndex(
                name: "IX_s2cell_center_lat",
                table: "s2cell",
                column: "center_lat");

            migrationBuilder.CreateIndex(
                name: "IX_s2cell_center_lon",
                table: "s2cell",
                column: "center_lon");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_spawnpoint_despawn_sec",
                table: "spawnpoint");

            migrationBuilder.DropIndex(
                name: "IX_spawnpoint_lat",
                table: "spawnpoint");

            migrationBuilder.DropIndex(
                name: "IX_spawnpoint_lon",
                table: "spawnpoint");

            migrationBuilder.DropIndex(
                name: "IX_s2cell_center_lat",
                table: "s2cell");

            migrationBuilder.DropIndex(
                name: "IX_s2cell_center_lon",
                table: "s2cell");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class AddApiKeyEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "pokemon",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_coords",
                table: "pokemon",
                columns: new[] { "lat", "lon" });

            migrationBuilder.CreateIndex(
                name: "ix_iv",
                table: "pokemon",
                columns: new[] { "atk_iv", "def_iv", "sta_iv" });

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_expire_timestamp",
                table: "pokemon",
                column: "expire_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_lat",
                table: "pokemon",
                column: "lat");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_level",
                table: "pokemon",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_lon",
                table: "pokemon",
                column: "lon");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_pokemon_id",
                table: "pokemon",
                column: "pokemon_id");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_updated",
                table: "pokemon",
                column: "updated");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_username",
                table: "pokemon",
                column: "username");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_coords",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "ix_iv",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_expire_timestamp",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_lat",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_level",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_lon",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_pokemon_id",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_updated",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_username",
                table: "pokemon");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "pokemon",
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

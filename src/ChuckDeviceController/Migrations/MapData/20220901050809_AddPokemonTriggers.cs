using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class AddPokemonTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "pokestop_id",
                table: "pokemon",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "trainer_name",
                table: "gym_defender",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "fort_id",
                table: "gym_defender",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokemon_hundo_stats",
                columns: table => new
                {
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    form_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    count = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon_hundo_stats", x => new { x.date, x.pokemon_id, x.form_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokemon_iv_stats",
                columns: table => new
                {
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    form_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    count = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon_iv_stats", x => new { x.date, x.pokemon_id, x.form_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokemon_shiny_stats",
                columns: table => new
                {
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    form_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    count = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon_shiny_stats", x => new { x.date, x.pokemon_id, x.form_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokemon_stats",
                columns: table => new
                {
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    form_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    count = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon_stats", x => new { x.date, x.pokemon_id, x.form_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_cell_id",
                table: "pokestop",
                column: "cell_id");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_cell_id",
                table: "pokemon",
                column: "cell_id");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_pokestop_id",
                table: "pokemon",
                column: "pokestop_id");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_spawn_id",
                table: "pokemon",
                column: "spawn_id");

            migrationBuilder.CreateIndex(
                name: "IX_gym_trainer_name",
                table: "gym_trainer",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_gym_defender_fort_id",
                table: "gym_defender",
                column: "fort_id");

            migrationBuilder.CreateIndex(
                name: "IX_gym_defender_trainer_name",
                table: "gym_defender",
                column: "trainer_name");

            migrationBuilder.CreateIndex(
                name: "IX_gym_cell_id",
                table: "gym",
                column: "cell_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gym_s2cell_cell_id",
                table: "gym",
                column: "cell_id",
                principalTable: "s2cell",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gym_defender_gym_fort_id",
                table: "gym_defender",
                column: "fort_id",
                principalTable: "gym",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_gym_defender_gym_trainer_trainer_name",
                table: "gym_defender",
                column: "trainer_name",
                principalTable: "gym_trainer",
                principalColumn: "name",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pokemon_pokestop_pokestop_id",
                table: "pokemon",
                column: "pokestop_id",
                principalTable: "pokestop",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pokemon_s2cell_cell_id",
                table: "pokemon",
                column: "cell_id",
                principalTable: "s2cell",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pokemon_spawnpoint_spawn_id",
                table: "pokemon",
                column: "spawn_id",
                principalTable: "spawnpoint",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pokestop_s2cell_cell_id",
                table: "pokestop",
                column: "cell_id",
                principalTable: "s2cell",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gym_s2cell_cell_id",
                table: "gym");

            migrationBuilder.DropForeignKey(
                name: "FK_gym_defender_gym_fort_id",
                table: "gym_defender");

            migrationBuilder.DropForeignKey(
                name: "FK_gym_defender_gym_trainer_trainer_name",
                table: "gym_defender");

            migrationBuilder.DropForeignKey(
                name: "FK_pokemon_pokestop_pokestop_id",
                table: "pokemon");

            migrationBuilder.DropForeignKey(
                name: "FK_pokemon_s2cell_cell_id",
                table: "pokemon");

            migrationBuilder.DropForeignKey(
                name: "FK_pokemon_spawnpoint_spawn_id",
                table: "pokemon");

            migrationBuilder.DropForeignKey(
                name: "FK_pokestop_s2cell_cell_id",
                table: "pokestop");

            migrationBuilder.DropTable(
                name: "pokemon_hundo_stats");

            migrationBuilder.DropTable(
                name: "pokemon_iv_stats");

            migrationBuilder.DropTable(
                name: "pokemon_shiny_stats");

            migrationBuilder.DropTable(
                name: "pokemon_stats");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_cell_id",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_cell_id",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_pokestop_id",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_spawn_id",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_gym_trainer_name",
                table: "gym_trainer");

            migrationBuilder.DropIndex(
                name: "IX_gym_defender_fort_id",
                table: "gym_defender");

            migrationBuilder.DropIndex(
                name: "IX_gym_defender_trainer_name",
                table: "gym_defender");

            migrationBuilder.DropIndex(
                name: "IX_gym_cell_id",
                table: "gym");

            migrationBuilder.AlterColumn<string>(
                name: "pokestop_id",
                table: "pokemon",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "gym_defender",
                keyColumn: "trainer_name",
                keyValue: null,
                column: "trainer_name",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "trainer_name",
                table: "gym_defender",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "gym_defender",
                keyColumn: "fort_id",
                keyValue: null,
                column: "fort_id",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "fort_id",
                table: "gym_defender",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

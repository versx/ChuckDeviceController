#nullable disable

namespace ChuckDeviceController.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddMapConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_gym_defender_gym_fort_id",
            table: "gym_defender");

        migrationBuilder.AlterColumn<string>(
            name: "pokestop_id",
            table: "incident",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "nickname",
            table: "gym_defender",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_s2cell_updated",
            table: "s2cell",
            column: "updated");

        migrationBuilder.AddForeignKey(
            name: "FK_gym_defender_gym_fort_id",
            table: "gym_defender",
            column: "fort_id",
            principalTable: "gym",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pokemon_spawnpoint_spawn_id",
            table: "pokemon",
            column: "spawn_id",
            principalTable: "spawnpoint",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_gym_defender_gym_fort_id",
            table: "gym_defender");

        migrationBuilder.DropForeignKey(
            name: "FK_pokemon_spawnpoint_spawn_id",
            table: "pokemon");

        migrationBuilder.DropIndex(
            name: "IX_s2cell_updated",
            table: "s2cell");

        migrationBuilder.UpdateData(
            table: "incident",
            keyColumn: "pokestop_id",
            keyValue: null,
            column: "pokestop_id",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "pokestop_id",
            table: "incident",
            type: "varchar(255)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.UpdateData(
            table: "gym_defender",
            keyColumn: "nickname",
            keyValue: null,
            column: "nickname",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "nickname",
            table: "gym_defender",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddForeignKey(
            name: "FK_gym_defender_gym_fort_id",
            table: "gym_defender",
            column: "fort_id",
            principalTable: "gym",
            principalColumn: "id");
    }
}

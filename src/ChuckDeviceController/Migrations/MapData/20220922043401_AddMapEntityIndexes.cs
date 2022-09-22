using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class AddMapEntityIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "quest_title",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_template",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_rewards",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_conditions",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_title",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_template",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_rewards",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_conditions",
                table: "pokestop",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_conditions",
                table: "pokestop",
                column: "alternative_quest_conditions");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_rewards",
                table: "pokestop",
                column: "alternative_quest_rewards");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_target",
                table: "pokestop",
                column: "alternative_quest_target");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_template",
                table: "pokestop",
                column: "alternative_quest_template");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_timestamp",
                table: "pokestop",
                column: "alternative_quest_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_title",
                table: "pokestop",
                column: "alternative_quest_title");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_alternative_quest_type",
                table: "pokestop",
                column: "alternative_quest_type");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_deleted",
                table: "pokestop",
                column: "deleted");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_enabled",
                table: "pokestop",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_lat",
                table: "pokestop",
                column: "lat");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_lon",
                table: "pokestop",
                column: "lon");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_conditions",
                table: "pokestop",
                column: "quest_conditions");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_rewards",
                table: "pokestop",
                column: "quest_rewards");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_target",
                table: "pokestop",
                column: "quest_target");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_template",
                table: "pokestop",
                column: "quest_template");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_timestamp",
                table: "pokestop",
                column: "quest_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_title",
                table: "pokestop",
                column: "quest_title");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_quest_type",
                table: "pokestop",
                column: "quest_type");

            migrationBuilder.CreateIndex(
                name: "IX_pokestop_updated",
                table: "pokestop",
                column: "updated");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_atk_iv",
                table: "pokemon",
                column: "atk_iv");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_def_iv",
                table: "pokemon",
                column: "def_iv");

            migrationBuilder.CreateIndex(
                name: "IX_pokemon_sta_iv",
                table: "pokemon",
                column: "sta_iv");

            migrationBuilder.CreateIndex(
                name: "IX_incident_expiration",
                table: "incident",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "IX_gym_deleted",
                table: "gym",
                column: "deleted");

            migrationBuilder.CreateIndex(
                name: "IX_gym_enabled",
                table: "gym",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "IX_gym_lat",
                table: "gym",
                column: "lat");

            migrationBuilder.CreateIndex(
                name: "IX_gym_lon",
                table: "gym",
                column: "lon");

            migrationBuilder.CreateIndex(
                name: "IX_gym_raid_end_timestamp",
                table: "gym",
                column: "raid_end_timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_gym_updated",
                table: "gym",
                column: "updated");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_conditions",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_rewards",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_target",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_template",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_timestamp",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_title",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_alternative_quest_type",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_deleted",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_enabled",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_lat",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_lon",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_conditions",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_rewards",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_target",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_template",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_timestamp",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_title",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_quest_type",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokestop_updated",
                table: "pokestop");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_atk_iv",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_def_iv",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_pokemon_sta_iv",
                table: "pokemon");

            migrationBuilder.DropIndex(
                name: "IX_incident_expiration",
                table: "incident");

            migrationBuilder.DropIndex(
                name: "IX_gym_deleted",
                table: "gym");

            migrationBuilder.DropIndex(
                name: "IX_gym_enabled",
                table: "gym");

            migrationBuilder.DropIndex(
                name: "IX_gym_lat",
                table: "gym");

            migrationBuilder.DropIndex(
                name: "IX_gym_lon",
                table: "gym");

            migrationBuilder.DropIndex(
                name: "IX_gym_raid_end_timestamp",
                table: "gym");

            migrationBuilder.DropIndex(
                name: "IX_gym_updated",
                table: "gym");

            migrationBuilder.AlterColumn<string>(
                name: "quest_title",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_template",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_rewards",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "quest_conditions",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_title",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_template",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_rewards",
                table: "pokestop",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "alternative_quest_conditions",
                table: "pokestop",
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

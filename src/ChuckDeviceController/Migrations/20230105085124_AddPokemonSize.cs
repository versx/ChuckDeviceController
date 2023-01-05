using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ushort>(
                name: "gameplay_condition",
                table: "weather",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<ushort>(
                name: "size",
                table: "pokemon",
                type: "smallint unsigned",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "height",
                table: "pokemon",
                type: "double",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AlterColumn<ushort>(
                name: "team_id",
                table: "gym_trainer",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "combat_rating",
                table: "gym_trainer",
                type: "double",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AlterColumn<uint>(
                name: "combat_rank",
                table: "gym_trainer",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AlterColumn<ushort>(
                name: "gender",
                table: "gym_defender",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<ushort>(
                name: "team_id",
                table: "gym",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "height",
                table: "pokemon");

            migrationBuilder.AlterColumn<int>(
                name: "gameplay_condition",
                table: "weather",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");

            migrationBuilder.AlterColumn<double>(
                name: "size",
                table: "pokemon",
                type: "double",
                precision: 18,
                scale: 6,
                nullable: true,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "team_id",
                table: "gym_trainer",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");

            migrationBuilder.AlterColumn<ulong>(
                name: "combat_rating",
                table: "gym_trainer",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<ulong>(
                name: "combat_rank",
                table: "gym_trainer",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int unsigned");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                table: "gym_defender",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");

            migrationBuilder.AlterColumn<int>(
                name: "team_id",
                table: "gym",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");
        }
    }
}

#nullable disable

namespace ChuckDeviceController.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class FixGymDefenderConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "gym_defender",
            keyColumn: "trainer_name",
            keyValue: null,
            column: "trainer_name",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "trainer_name",
            table: "gym_defender",
            type: "varchar(255)",
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
            type: "varchar(255)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "trainer_name",
            table: "gym_defender",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "fort_id",
            table: "gym_defender",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }
}

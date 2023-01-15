#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddPluginTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "webhook",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "instance",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "plugin",
            columns: table => new
            {
                name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                state = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plugin", x => x.name);
            })
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "plugin");

        migrationBuilder.UpdateData(
            table: "webhook",
            keyColumn: "data",
            keyValue: null,
            column: "data",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "webhook",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.UpdateData(
            table: "instance",
            keyColumn: "data",
            keyValue: null,
            column: "data",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "instance",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }
}

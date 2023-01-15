#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class ChangeWebhookType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "types",
            table: "webhook",
            type: "int",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "types",
            table: "webhook",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int")
            .Annotation("MySql:CharSet", "utf8mb4");
    }
}

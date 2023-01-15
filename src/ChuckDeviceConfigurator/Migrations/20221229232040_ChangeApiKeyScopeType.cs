#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class ChangeApiKeyScopeType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "full_path",
            table: "plugin",
            type: "longtext",
            nullable: false)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "geofence",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "instance_name",
            table: "device",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "account_username",
            table: "device",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<int>(
            name: "scope",
            table: "api_key",
            type: "int",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_device_account_username",
            table: "device",
            column: "account_username");

        migrationBuilder.CreateIndex(
            name: "IX_device_instance_name",
            table: "device",
            column: "instance_name");

        migrationBuilder.CreateIndex(
            name: "IX_device_last_seen",
            table: "device",
            column: "last_seen");

        migrationBuilder.AddForeignKey(
            name: "FK_device_account_account_username",
            table: "device",
            column: "account_username",
            principalTable: "account",
            principalColumn: "username");
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
            name: "IX_device_instance_name",
            table: "device");

        migrationBuilder.DropIndex(
            name: "IX_device_last_seen",
            table: "device");

        migrationBuilder.DropColumn(
            name: "full_path",
            table: "plugin");

        migrationBuilder.UpdateData(
            table: "geofence",
            keyColumn: "data",
            keyValue: null,
            column: "data",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "data",
            table: "geofence",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "instance_name",
            table: "device",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "account_username",
            table: "device",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "scope",
            table: "api_key",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int")
            .Annotation("MySql:CharSet", "utf8mb4");
    }
}

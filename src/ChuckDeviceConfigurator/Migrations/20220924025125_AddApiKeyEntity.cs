#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddApiKeyEntity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "instance_name",
            table: "assignment",
            type: "varchar(255)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "api_key",
            columns: table => new
            {
                id = table.Column<uint>(type: "int unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                key = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                scope = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                expiration_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_api_key", x => x.id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_assignment_instance_name",
            table: "assignment",
            column: "instance_name");

        migrationBuilder.CreateIndex(
            name: "IX_api_key_expiration_timestamp",
            table: "api_key",
            column: "expiration_timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "api_key");

        migrationBuilder.DropIndex(
            name: "IX_assignment_instance_name",
            table: "assignment");

        migrationBuilder.AlterColumn<string>(
            name: "instance_name",
            table: "assignment",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(255)")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }
}

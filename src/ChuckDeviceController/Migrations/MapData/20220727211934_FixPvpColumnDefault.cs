using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class FixPvpColumnDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "pvp",
                table: "pokemon",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "pokemon",
                keyColumn: "pvp",
                keyValue: null,
                column: "pvp",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "pvp",
                table: "pokemon",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

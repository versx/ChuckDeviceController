using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations
{
    public partial class AddPvpColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pvp",
                table: "pokemon",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pvp",
                table: "pokemon");
        }
    }
}

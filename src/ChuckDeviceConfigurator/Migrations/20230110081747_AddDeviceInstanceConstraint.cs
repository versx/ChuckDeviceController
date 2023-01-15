#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddDeviceInstanceConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_device_instance_instance_name",
            table: "device",
            column: "instance_name",
            principalTable: "instance",
            principalColumn: "name",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_device_instance_instance_name",
            table: "device");
    }
}

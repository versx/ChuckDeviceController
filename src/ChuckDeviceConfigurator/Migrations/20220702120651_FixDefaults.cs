#nullable disable

namespace ChuckDeviceConfigurator.Migrations;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class FixDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "account",
            columns: table => new
            {
                username = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                password = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                first_warning_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                failed_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                failed = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                last_encounter_time = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                last_encounter_lat = table.Column<double>(type: "double", nullable: true),
                last_encounter_lon = table.Column<double>(type: "double", nullable: true),
                spins = table.Column<uint>(type: "int unsigned", nullable: false),
                tutorial = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                creation_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                warn = table.Column<bool>(type: "tinyint(1)", nullable: true),
                warn_expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                warn_message_acknowledged = table.Column<bool>(type: "tinyint(1)", nullable: true),
                suspended_message_acknowledged = table.Column<bool>(type: "tinyint(1)", nullable: true),
                was_suspended = table.Column<bool>(type: "tinyint(1)", nullable: true),
                banned = table.Column<bool>(type: "tinyint(1)", nullable: true),
                last_used_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                group = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account", x => x.username);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "assignment",
            columns: table => new
            {
                id = table.Column<uint>(type: "int unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                instance_name = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                source_instance_name = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                device_uuid = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                time = table.Column<uint>(type: "int unsigned", nullable: false),
                date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                device_group_name = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_assignment", x => x.id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "device",
            columns: table => new
            {
                uuid = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                instance_name = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                account_username = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                last_host = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                last_lat = table.Column<double>(type: "double", nullable: true),
                last_lon = table.Column<double>(type: "double", nullable: true),
                last_seen = table.Column<ulong>(type: "bigint unsigned", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_device", x => x.uuid);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "geofence",
            columns: table => new
            {
                name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                type = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                data = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_geofence", x => x.name);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "instance",
            columns: table => new
            {
                name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                type = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                min_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                max_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                geofences = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                data = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_instance", x => x.name);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "iv_list",
            columns: table => new
            {
                name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                pokemon_ids = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_iv_list", x => x.name);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "webhook",
            columns: table => new
            {
                name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                types = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                delay = table.Column<double>(type: "double", nullable: false),
                url = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                geofences = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                data = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_webhook", x => x.name);
            })
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "account");

        migrationBuilder.DropTable(
            name: "assignment");

        migrationBuilder.DropTable(
            name: "device");

        migrationBuilder.DropTable(
            name: "geofence");

        migrationBuilder.DropTable(
            name: "instance");

        migrationBuilder.DropTable(
            name: "iv_list");

        migrationBuilder.DropTable(
            name: "webhook");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChuckDeviceController.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    username = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    password = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    first_warning_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    failed_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    failed = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    last_encounter_time = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    last_encounter_lat = table.Column<double>(type: "double", nullable: true),
                    last_encounter_lon = table.Column<double>(type: "double", nullable: true),
                    spins = table.Column<uint>(type: "int unsigned", nullable: false),
                    tutorial = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.username);
                });

            migrationBuilder.CreateTable(
                name: "assignment",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    instance_name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    source_instance_name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    device_uuid = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    time = table.Column<uint>(type: "int unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    uuid = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    instance_name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    account_username = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    last_host = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    last_lat = table.Column<double>(type: "double", nullable: true),
                    last_lon = table.Column<double>(type: "double", nullable: true),
                    last_seen = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "gym",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    url = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    last_modified_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    raid_end_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    raid_spawn_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    raid_battle_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    raid_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    guarding_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    availble_slots = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    raid_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ex_raid_eligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    in_battle = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    raid_pokemon_move_1 = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_move_2 = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_form = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_costume = table.Column<uint>(type: "int unsigned", nullable: false),
                    raid_pokemon_evolution = table.Column<uint>(type: "int unsigned", nullable: false),
                    raid_pokemon_gender = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    raid_pokemon_cp = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_is_exclusive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    total_cp = table.Column<int>(type: "int", nullable: false),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    sponsor_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gym_defender",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    pokemon_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    cp_when_deployed = table.Column<uint>(type: "int unsigned", nullable: false),
                    cp_now = table.Column<uint>(type: "int unsigned", nullable: false),
                    berry_value = table.Column<double>(type: "double", nullable: false),
                    times_fed = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    deployment_duration = table.Column<uint>(type: "int unsigned", nullable: false),
                    trainer_name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    fort_id = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    atk_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    def_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    sta_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    move_1 = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    move_2 = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    battles_attacked = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    battles_defended = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    gender = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    hatched_from_egg = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    pvp_combat_won = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    pvp_combat_total = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    npc_combat_won = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    npc_combat_total = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_defender", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instance",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    type = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    data = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "pokemon",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    pokestop_id = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    spawn_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    weight = table.Column<double>(type: "double", nullable: true),
                    size = table.Column<double>(type: "double", nullable: true),
                    expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    move_1 = table.Column<uint>(type: "int unsigned", nullable: true),
                    move_2 = table.Column<uint>(type: "int unsigned", nullable: true),
                    gender = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    cp = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    atk_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    def_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    sta_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    form = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    weather = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    costume = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    changed = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    expire_timestamp_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    shiny = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    username = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    display_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    capture_1 = table.Column<double>(type: "double", nullable: true),
                    capture_2 = table.Column<double>(type: "double", nullable: true),
                    capture_3 = table.Column<double>(type: "double", nullable: true),
                    pvp_rankings_great_league = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    pvp_rankings_ultra_league = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    is_event = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pokestop",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    url = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    lure_expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    last_modified_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    quest_type = table.Column<int>(type: "int", nullable: true),
                    quest_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    quest_target = table.Column<uint>(type: "int unsigned", nullable: true),
                    quest_conditions = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    quest_rewards = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    quest_template = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    lure_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pokestop_display = table.Column<uint>(type: "int unsigned", nullable: true),
                    incident_expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    grunt_type = table.Column<uint>(type: "int unsigned", nullable: true),
                    sponsor_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ar_scan_eligible = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokestop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "s2cell",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    center_lat = table.Column<double>(type: "double", nullable: false),
                    center_lon = table.Column<double>(type: "double", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s2cell", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spawnpoint",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    despawn_sec = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpoint", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trainer",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", nullable: false),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    team_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    battles_won = table.Column<uint>(type: "int unsigned", nullable: false),
                    km_walked = table.Column<double>(type: "double", nullable: false),
                    pokemon_caught = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    experience = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    combat_rank = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    combat_rating = table.Column<double>(type: "double", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "weather",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: false),
                    longitude = table.Column<double>(type: "double", nullable: false),
                    gameplay_condition = table.Column<int>(type: "int", nullable: false),
                    wind_direction = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    cloud_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    rain_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    wind_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    snow_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    fog_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    special_effect_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    severity = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    warn_weather = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather", x => x.id);
                });
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
                name: "gym");

            migrationBuilder.DropTable(
                name: "gym_defender");

            migrationBuilder.DropTable(
                name: "instance");

            migrationBuilder.DropTable(
                name: "pokemon");

            migrationBuilder.DropTable(
                name: "pokestop");

            migrationBuilder.DropTable(
                name: "s2cell");

            migrationBuilder.DropTable(
                name: "spawnpoint");

            migrationBuilder.DropTable(
                name: "trainer");

            migrationBuilder.DropTable(
                name: "weather");
        }
    }
}

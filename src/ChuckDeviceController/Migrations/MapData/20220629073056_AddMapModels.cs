using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChuckDeviceController.Migrations.MapData
{
    public partial class AddMapModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gym",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    raid_end_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    raid_spawn_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    raid_battle_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    raid_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    guarding_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    available_slots = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    raid_level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ex_raid_eligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    in_battle = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    raid_pokemon_move_1 = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_move_2 = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_form = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_costume = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_cp = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_evolution = table.Column<uint>(type: "int unsigned", nullable: true),
                    raid_pokemon_gender = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    raid_is_exclusive = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    total_cp = table.Column<int>(type: "int", nullable: false),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    sponsor_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ar_scan_eligible = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    power_up_points = table.Column<uint>(type: "int unsigned", nullable: true),
                    power_up_level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    power_up_end_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gym_defender",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    nickname = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pokemon_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    display_pokemon_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    form = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    costume = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    gender = table.Column<int>(type: "int", nullable: false),
                    cp_when_deployed = table.Column<uint>(type: "int unsigned", nullable: false),
                    cp_now = table.Column<uint>(type: "int unsigned", nullable: false),
                    cp = table.Column<uint>(type: "int unsigned", nullable: false),
                    battles_won = table.Column<uint>(type: "int unsigned", nullable: false),
                    battles_lost = table.Column<uint>(type: "int unsigned", nullable: false),
                    berry_value = table.Column<double>(type: "double", nullable: false),
                    times_fed = table.Column<uint>(type: "int unsigned", nullable: false),
                    deployment_duration = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    trainer_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fort_id = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    atk_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    def_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    sta_iv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    move_1 = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    move_2 = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    move_3 = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    battles_attacked = table.Column<uint>(type: "int unsigned", nullable: false),
                    battles_defended = table.Column<uint>(type: "int unsigned", nullable: false),
                    buddy_km_walked = table.Column<double>(type: "double", nullable: false),
                    buddy_candy_awarded = table.Column<uint>(type: "int unsigned", nullable: false),
                    coins_returned = table.Column<uint>(type: "int unsigned", nullable: false),
                    from_fort = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    hatched_from_egg = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_bad = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_egg = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_lucky = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    shiny = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    pvp_combat_won = table.Column<uint>(type: "int unsigned", nullable: false),
                    pvp_combat_total = table.Column<uint>(type: "int unsigned", nullable: false),
                    npc_combat_won = table.Column<uint>(type: "int unsigned", nullable: false),
                    npc_combat_total = table.Column<uint>(type: "int unsigned", nullable: false),
                    height_m = table.Column<double>(type: "double", nullable: false),
                    weight_kg = table.Column<double>(type: "double", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_defender", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gym_trainer",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    battles_won = table.Column<uint>(type: "int unsigned", nullable: false),
                    km_walked = table.Column<double>(type: "double", nullable: false),
                    pokemon_caught = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    experience = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    combat_rank = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    combat_rating = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    has_shared_ex_pass = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    gym_badge_type = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_trainer", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokemon",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pokemon_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    spawn_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    atk_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    def_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    sta_iv = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    iv = table.Column<double>(type: "double", nullable: true, computedColumnSql: "(`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45"),
                    move_1 = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    move_2 = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    gender = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    form = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    costume = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    cp = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    weight = table.Column<double>(type: "double", nullable: true),
                    size = table.Column<double>(type: "double", nullable: true),
                    weather = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    shiny = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pokestop_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    changed = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    expire_timestamp_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    capture_1 = table.Column<double>(type: "double", nullable: true),
                    capture_2 = table.Column<double>(type: "double", nullable: true),
                    capture_3 = table.Column<double>(type: "double", nullable: true),
                    is_ditto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    display_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    base_height = table.Column<double>(type: "double", nullable: false),
                    base_weight = table.Column<double>(type: "double", nullable: false),
                    is_event = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    seen_type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokemon", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pokestop",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lure_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    lure_expire_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    last_modified_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cell_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    first_seen_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    sponsor_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ar_scan_eligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    power_up_points = table.Column<uint>(type: "int unsigned", nullable: true),
                    power_up_level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    power_up_end_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    quest_type = table.Column<uint>(type: "int unsigned", nullable: true),
                    quest_template = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quest_title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quest_target = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    quest_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    quest_reward_type = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`quest_rewards`,'$[*].type'),'$[0]')"),
                    quest_item_id = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`quest_rewards`,'$[*].info.item_id'),'$[0]')"),
                    quest_reward_amount = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`quest_rewards`,'$[*].info.amount'),'$[0]')"),
                    quest_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`quest_rewards`,'$[*].info.pokemon_id'),'$[0]')"),
                    quest_conditions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quest_rewards = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alternative_quest_type = table.Column<uint>(type: "int unsigned", nullable: true),
                    alternative_quest_template = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alternative_quest_title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alternative_quest_target = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    alternative_quest_timestamp = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    alternative_quest_reward_type = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`alternative_quest_rewards`,'$[*].type'),'$[0]')"),
                    alternative_quest_item_id = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.item_id'),'$[0]')"),
                    alternative_quest_reward_amount = table.Column<ushort>(type: "smallint unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.amount'),'$[0]')"),
                    alternative_quest_pokemon_id = table.Column<uint>(type: "int unsigned", nullable: true, computedColumnSql: "json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.pokemon_id'),'$[0]')"),
                    alternative_quest_conditions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alternative_quest_rewards = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pokestop", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "s2cell",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    center_lat = table.Column<double>(type: "double", nullable: false),
                    center_lon = table.Column<double>(type: "double", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s2cell", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "spawnpoint",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    lat = table.Column<double>(type: "double", nullable: false),
                    lon = table.Column<double>(type: "double", nullable: false),
                    despawn_sec = table.Column<uint>(type: "int unsigned", nullable: true),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    last_seen = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpoint", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "weather",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incident",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pokestop_id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    expiration = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    display_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    style = table.Column<uint>(type: "int unsigned", nullable: false),
                    character = table.Column<uint>(type: "int unsigned", nullable: false),
                    updated = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_pokestop_pokestop_id",
                        column: x => x.pokestop_id,
                        principalTable: "pokestop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_incident_pokestop_id",
                table: "incident",
                column: "pokestop_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gym");

            migrationBuilder.DropTable(
                name: "gym_defender");

            migrationBuilder.DropTable(
                name: "gym_trainer");

            migrationBuilder.DropTable(
                name: "incident");

            migrationBuilder.DropTable(
                name: "pokemon");

            migrationBuilder.DropTable(
                name: "s2cell");

            migrationBuilder.DropTable(
                name: "spawnpoint");

            migrationBuilder.DropTable(
                name: "weather");

            migrationBuilder.DropTable(
                name: "pokestop");
        }
    }
}

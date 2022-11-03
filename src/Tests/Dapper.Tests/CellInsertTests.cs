namespace Dapper.Tests
{
    using System.Data;
    using System.Diagnostics;

    using Dapper;
    using Microsoft.EntityFrameworkCore;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    public class CellInsertTests
    {
        private const string ConnectionString = "Server=10.0.1.100;Port=3307;Uid=versx;Password=jerm5301;Database=cdc_test3;";

        private MySqlConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new MySqlConnection(ConnectionString);
        }

        [Test]
        public async Task Test1()
        {
            //Assert.Pass();
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                //var cellsSql = cellsToUpsert
                //    .SelectMany(x => x.Value)
                //    .Select(cell => $"({cell.Id}, {cell.Level}, {cell.Latitude}, {cell.Longitude}, UNIX_TIMESTAMP())");
                //var args = string.Join(",", cellsSql);
                //var sql = string.Format(SqlQueries.S2Cells, args);

                //var cells = cellsToUpsert[0].Value;
                var now = DateTime.UtcNow.ToTotalSeconds();
                var cells = new List<Cell>();
                for (var i = 0; i < 1000000; i++)
                {
                    cells.Add(new Cell
                    {
                        Id = (ulong)i + 1,
                        Latitude = i + 1,
                        Longitude = i + 1,
                        Level = 15,
                        Updated = now,
                    });
                }
                await InsertInBulk(cells);

                stopwatch.Stop();
                var seconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                Console.WriteLine($"[Cell] Upserted {cells.Count:N0} S2 cells in {seconds}s");
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cell] Error: {ex.InnerException?.Message ?? ex.Message}");
                Assert.Fail(ex.ToString());
            }
        }

        public async Task InsertInBulk(List<Cell> entities)
        {
            var sqls = GetSqlsInBatches(entities);
            foreach (var sql in sqls)
            {
                await _connection.ExecuteAsync(sql);
            }
        }

        public async Task InsertInBulk(List<Weather> entities)
        {
            var sqls = GetSqlsInBatches(entities);
            foreach (var sql in sqls)
            {
                await _connection.ExecuteAsync(sql);
            }
        }

        //public async Task SafeInsertMany(IEnumerable<string> userNames)
        //{
        //    using (var connection = new MySqlConnection(_connectionString))
        //    {
        //        var parameters = userNames.Select(u =>
        //        {
        //            var tempParams = new DynamicParameters();
        //            tempParams.Add("@Name", u, DbType.String, ParameterDirection.Input);
        //            return tempParams;
        //        });

        //        await connection.ExecuteAsync(
        //            "INSERT INTO [Users] (Name, LastUpdatedAt) VALUES (@Name, getdate())",
        //            parameters).ConfigureAwait(false);
        //    }
        //}



        private IList<string> GetSqlsInBatches(List<Cell> entities)
        {
            //lock (_upsertLock)
            {
                var insertSql = "INSERT INTO s2cell (id, level, center_lat, center_lon, updated) VALUES ";
                var valuesSql = "('{0}', '{1}', '{2}', '{3}', UNIX_TIMESTAMP())";
                var onDupSql = @"
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    center_lat=VALUES(center_lat),
    center_lon=VALUES(center_lon),
    updated=VALUES(updated)
";
                var batchSize = 1000;

                var sqlsToExecute = new List<string>();
                var numberOfBatches = (int)Math.Ceiling((double)entities.Count / batchSize);

                for (var i = 0; i < numberOfBatches; i++)
                {
                    var entitiesToInsert = entities.Skip(i * batchSize).Take(batchSize);
                    var valuesToInsert = entitiesToInsert.Select(x => string.Format(valuesSql, x.Id, x.Level, x.Latitude, x.Longitude));
                    sqlsToExecute.Add(insertSql + string.Join(',', valuesToInsert) + onDupSql);
                }

                return sqlsToExecute;
            }
        }

        private IList<string> GetSqlsInBatches(List<Weather> entities)
        {
            //lock (_upsertLock)
            {
                var insertSql = "INSERT INTO weather (id, level, latitude, longitude, gameplay_condition, cloud_level, rain_level, snow_level, fog_level, wind_level, wind_direction, warn_weather, special_effect_level, severity, updated) VALUES ";
                // 0=id
                // 1=level
                // 2=latitude
                // 3=longitude
                // 4=gameplay_condition
                // 5=cloud_level
                // 6=rain_level
                // 7=snow_level
                // 8=fog_level
                // 9=wind_level
                // 10=wind_direction
                // 11=warn_weather
                // 12=special_effect_level
                // 13=severity
                // 14=updated
                var valuesSql = "('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', UNIX_TIMESTAMP())";
                var onDupSql = @"
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    latitude=VALUES(latitude),
    longitude=VALUES(longitude),
    gameplay_condition=VALUES(gameplay_condition),
    wind_direction=VALUES(wind_direction),
    cloud_level=VALUES(cloud_level),
    rain_level=VALUES(rain_level),
    wind_level=VALUES(wind_level),
    snow_level=VALUES(snow_level),
    fog_level=VALUES(fog_level),
    special_effect_level=VALUES(special_effect_level),
    severity=VALUES(severity),
    warn_weather=VALUES(warn_weather),
    updated=VALUES(updated)
";
                var batchSize = 1000;

                var sqlsToExecute = new List<string>();
                var numberOfBatches = (int)Math.Ceiling((double)entities.Count / batchSize);

                for (var i = 0; i < numberOfBatches; i++)
                {
                    var entitiesToInsert = entities.Skip(i * batchSize).Take(batchSize);
                    var valuesToInsert = entitiesToInsert.Select(x => string.Format(valuesSql, x.Id, x.Level, x.Latitude, x.Longitude, (int)x.GameplayCondition, x.CloudLevel, x.RainLevel, x.SnowLevel, x.FogLevel, x.WindLevel, x.WindDirection, x.WarnWeather, x.SpecialEffectLevel, x.Severity));
                    sqlsToExecute.Add(insertSql + string.Join(',', valuesToInsert) + onDupSql);
                }

                return sqlsToExecute;
            }
        }
    }
}
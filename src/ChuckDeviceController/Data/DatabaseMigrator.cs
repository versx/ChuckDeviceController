namespace ChuckDeviceController.Data
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;
    using Chuck.Infrastructure.Data.Repositories;

    /// <summary>
    /// Database migration class
    /// </summary>
    public class DatabaseMigrator
    {
        private static readonly ILogger<DatabaseMigrator> _logger =
            new Logger<DatabaseMigrator>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly MetadataRepository _metadataRepository;

        /// <summary>
        /// Gets a value determining whether the migration has finished or not
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// Gets the migrations folder path
        /// </summary>
        public string MigrationsFolder => Path.Combine
        (
            Directory.GetCurrentDirectory(),
            Strings.MigrationsFolder
        );

        /// <summary>
        /// Instantiates a new <see cref="DatabaseMigrator"/> class
        /// </summary>
        public DatabaseMigrator()
        {
            _metadataRepository = new MetadataRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));

            // Create the metadata table
            _metadataRepository.ExecuteSql(Strings.SQL_CREATE_TABLE_METADATA);

            // Get current version from metadata table
            var dbVersion = GetMetadata("DB_VERSION").ConfigureAwait(false)
                                                     .GetAwaiter()
                                                     .GetResult();
            var currentVersion = int.Parse(dbVersion?.Value ?? "0");

            // Get newest version from migration files
            var newestVersion = GetNewestDbVersion();
            _logger.LogInformation($"Current: {currentVersion}, Latest: {newestVersion}");

            // Attempt to migrate the database
            if (currentVersion < newestVersion)
            {
                // Wait 30 seconds and let user know we are about to migrate the database and for them to make
                // a backup until we handle backups and rollbacks.
                _logger.LogInformation("MIGRATION IS ABOUT TO START IN 30 SECONDS, PLEASE MAKE SURE YOU HAVE A BACKUP!!!");
                Thread.Sleep(30 * 1000);
            }
            Migrate(currentVersion, newestVersion).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get newest database version from local migration file numbers
        /// </summary>
        /// <returns>Returns the latest version number</returns>
        private int GetNewestDbVersion()
        {
            var current = 0;
            var keepChecking = true;
            while (keepChecking)
            {
                var path = Path.Combine(MigrationsFolder, current + 1 + ".sql");
                if (File.Exists(path))
                    current++;
                else
                    keepChecking = false;
            }
            return current;
        }

        /// <summary>
        /// Migrate the database from a specified version to the next version
        /// </summary>
        /// <param name="fromVersion">Database version to migrate from</param>
        /// <param name="toVersion">Database version to migrate to</param>
        /// <returns></returns>
        private async Task Migrate(int fromVersion, int toVersion)
        {
            if (fromVersion < toVersion)
            {
                _logger.LogInformation($"Migrating database to version {fromVersion + 1}");
                var sqlFile = Path.Combine(MigrationsFolder, fromVersion + 1 + ".sql");

                // Read SQL file and remove any new lines
                var migrateSql = File.ReadAllText(sqlFile)?.Replace("\r", "").Replace("\n", "");

                // If the migration file contains multiple queries, split them up
                var sqlSplit = migrateSql.Split(';');

                // Loop through the migration queries
                foreach (var sql in sqlSplit)
                {
                    // If the SQL query is null, skip...
                    if (string.IsNullOrEmpty(sql))
                        continue;

                    // Execute the SQL query
                    var result = _metadataRepository.ExecuteSql(sql);
                    if (!result)
                    {
                        // Failed to execute query
                        _logger.LogWarning($"Failed to execute migration: {sql}");
                        continue;
                    }
                    _logger.LogDebug($"Migration execution result: {result}");
                }

                // Take a break
                Thread.Sleep(2000);

                // Build query to update metadata table version key
                var newVersion = fromVersion + 1;
                try
                {
                    await _metadataRepository.AddOrUpdateAsync(new Metadata { Key = "DB_VERSION", Value = newVersion.ToString() }).ConfigureAwait(false);
                    _logger.LogInformation("Migration successful");
                }
                catch (Exception ex)
                {
                    // Failed migration
                    _logger.LogError($"Failed migration err: {ex.Message}");
                }
                await Migrate(newVersion, toVersion).ConfigureAwait(false);
            }
            if (fromVersion == toVersion)
            {
                _logger.LogInformation("Migration done");
                Finished = true;
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Get a metadata table value by key
        /// </summary>
        /// <param name="key">Table key to lookup</param>
        /// <returns>Returns the metadata key and value</returns>
        public async Task<Metadata> GetMetadata(string key)
        {
            return await _metadataRepository.GetByIdAsync(key).ConfigureAwait(false);
        }
    }
}
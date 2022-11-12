namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using System.Threading.Tasks;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Plugin;

    public class GeofenceServiceHost : IGeofenceServiceHost
    {
        private static readonly ILogger<IGeofenceServiceHost> _logger =
            new Logger<IGeofenceServiceHost>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly string _connectionString;

        public GeofenceServiceHost(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task CreateGeofenceAsync(IGeofence options)
        {
            using (var context = DbContextFactory.CreateControllerContext(_connectionString))
            {
                /* TODO: Either keep this conditional check and create a separate Update method or remove it and reuse the same method
                if (context.Geofences.Any(g => g.Name == options.Name))
                {
                    _logger.LogError($"Geofence already exists with name '{options.Name}', failed to create geofence.");
                    return;
                }
                */

                var geofence = new Geofence
                {
                    Name = options.Name,
                    Type = options.Type,
                    Data = new GeofenceData
                    {
                        Area = options.Data?.Area,
                    },
                };

                await context.SingleMergeAsync(geofence, options =>
                {
                    options.UseTableLock = true;
                    options.OnMergeUpdateInputExpression = p => new
                    {
                        p.Type,
                        p.Data,
                    };
                });
            }
        }
    }
}
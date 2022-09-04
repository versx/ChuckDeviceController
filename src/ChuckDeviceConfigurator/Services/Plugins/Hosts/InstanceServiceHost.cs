namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    /*
    using System.Threading.Tasks;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Plugin;

    public class InstanceServiceHost : IInstanceServiceHost
    {
        private static readonly ILogger<IInstanceServiceHost> _logger =
            new Logger<IInstanceServiceHost>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly string _connectionString;

        public InstanceServiceHost(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task CreateInstanceTypeAsync(IInstanceCreationOptions options)
        {
            using (var context = DbContextFactory.CreateControllerContext(_connectionString))
            {
                if (context.Instances.Any(i => i.Name == options.Name))
                {
                    _logger.LogError($"Instance already exists with name '{options.Name}', failed to create instance.");
                    return;
                }

                // TODO: Add relation to JobInstanceController from plugin
                // TODO: Allow plugins to create instances to link with job controllers, that way they are easily used via the UI
                var instance = new Instance
                {
                    Name = options.Name,
                    // TODO: When InstanceType.Custom selected via UI - maybe show a separate select listing available job controllers from plugins (add InstanceData property for 'custom_instance_name' or something)
                    Type = InstanceType.Custom,
                    Geofences = options.Geofences,
                    MinimumLevel = options.MinimumLevel,
                    MaximumLevel = options.MaximumLevel,
                    Data = new InstanceData
                    {
                        AccountGroup = options.GroupName,
                        IsEvent = options.IsEvent,
                    },
                };

                await context.Instances.AddAsync(instance);
                await context.SaveChangesAsync();
            }
        }
    }
    */
}
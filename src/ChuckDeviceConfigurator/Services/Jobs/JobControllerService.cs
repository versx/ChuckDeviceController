namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    using Microsoft.EntityFrameworkCore;

    // TODO: HostedService?
    public class JobControllerService : IJobControllerService
    {
        #region Variables

        private readonly ILogger<IJobControllerService> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _factory;

        private readonly IDictionary<string, Device> _devices;
        private readonly IDictionary<string, IJobController> _instances;

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, Device> Devices =>
            (IReadOnlyDictionary<string, Device>)_devices;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, IJobController> Instances =>
            (IReadOnlyDictionary<string, IJobController>)_instances;

        #endregion

        public JobControllerService(
            ILogger<IJobControllerService> logger,
            IDbContextFactory<DeviceControllerContext> factory)
        {
            _logger = logger;
            _factory = factory;

            _devices = new Dictionary<string, Device>();
            _instances = new Dictionary<string, IJobController>();

            Start();
        }

        public void Start()
        {
            using (var context = _factory.CreateDbContext())
            {
                var instances = context.Instances.ToList();
                var devices = context.Devices.ToList();

                foreach (var instance in instances)
                {
                    if (!ThreadPool.QueueUserWorkItem(async _ =>
                    {
                        _logger.LogInformation($"Starting instance {instance.Name}");
                        await AddInstanceAsync(instance);
                        _logger.LogInformation($"Started instance {instance.Name}");

                        var newDevices = devices.AsEnumerable()
                                                .Where(d => string.Compare(d.InstanceName, instance.Name, true) == 0);
                        foreach (var device in newDevices)
                        {
                            AddDevice(device);
                        }
                    }))
                    {
                        _logger.LogError($"Failed to start instance {instance.Name}");
                    }
                }
            }
            _logger.LogInformation("All instances have been started");
        }

        public void Stop()
        {
            // TODO: JobControllerService.Stop
        }

        #region Instances

        public async Task AddInstanceAsync(Instance instance)
        {
            // TODO: JobControllerService.AddInstance
            _logger.LogDebug($"Adding instance {instance.Name}");
            await Task.CompletedTask;
        }

        public IJobController GetInstanceController(string uuid)
        {
            // TODO: JobControllerService.GetInstanceController
            return null;
        }

        public async Task<string> GetStatusAsync(Instance instance)
        {
            // TODO: JobControllerService.GetStatus
            return null;
        }

        public void ReloadAll()
        {
            // TODO: JobControllerService.ReloadAll
        }

        public async Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName)
        {
            // TODO: JobControllerService.ReloadInstance
            await Task.CompletedTask;
        }

        public async Task RemoveInstanceAsync(string instanceName)
        {
            // TODO: JobControllerService.RemoveInstance
            await Task.CompletedTask;
        }

        #endregion

        #region Devices

        public void AddDevice(Device device)
        {
            // TODO: JobControllerService.AddDevice
        }

        public List<string> GetDeviceUuidsInInstance(string instanceName)
        {
            // TODO: JobControllerService.GetDeviceUuidsInInstance
            return null;
        }

        public void ReloadDevice(Device device, string oldDeviceUuid)
        {
            // TODO: JobControllerService.ReloadDevice
        }

        public Task RemoveDeviceAsync(Device device)
        {
            // TODO: JobControllerService.RemoveDevice
            return null;
        }

        public void RemoveDevice(string uuid)
        {
            // TODO: JobControllerService.RemoveDevice
        }

        #endregion
    }
}
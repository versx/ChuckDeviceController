namespace ChuckDeviceConfigurator.Services.Assignments
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class AssignmentControllerService : IAssignmentControllerService
    {
        #region Variables

        private readonly IDbContextFactory<DeviceControllerContext> _factory;
        private readonly ILogger<IAssignmentControllerService> _logger;
        private readonly IJobControllerService _jobControllerService;

        private readonly object _assignmentsLock = new();
        private readonly System.Timers.Timer _timer;
        private List<Assignment> _assignments;
        private bool _initialized;
        private long _lastUpdated;

        #endregion

        #region Constructor

        public AssignmentControllerService(
            ILogger<IAssignmentControllerService> logger,
            IDbContextFactory<DeviceControllerContext> factory,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _factory = factory;
            _jobControllerService = jobControllerService;

            _lastUpdated = -2;
            _assignments = new List<Assignment>();
            _timer = new System.Timers.Timer(5 * 1000); // 5 second interval
            _timer.Elapsed += async (sender, e) => await CheckAssignments();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            Reload();

            if (!_initialized)
            {
                _logger.LogInformation($"Starting {nameof(AssignmentControllerService)}...");
                _timer.Start();
                _initialized = true;
            }
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reload()
        {
            // Reload all available device assignments
            var assignments = GetAssignments();
            lock (_assignmentsLock)
            {
                _assignments = assignments;
            }
        }

        public void AddAssignment(Assignment assignment)
        {
            lock (_assignmentsLock)
            {
                _assignments.Add(assignment);
            }
        }

        public void EditAssignment(uint oldAssignmentId, Assignment newAssignment)
        {
            DeleteAssignment(oldAssignmentId);
            AddAssignment(newAssignment);
        }

        public void DeleteAssignment(Assignment assignment)
        {
            DeleteAssignment(assignment.Id);
        }

        public void DeleteAssignment(uint id)
        {
            lock (_assignmentsLock)
            {
                _assignments = _assignments.Where(x => x.Id != id)
                                           .ToList();
            }
        }

        #endregion

        #region Private Methods

        private async Task CheckAssignments()
        {
            var dateNow = DateTime.Now;
            var now = dateNow.Hour * 3600 + dateNow.Minute * 60 + dateNow.Second;
            if (_lastUpdated == -2)
            {
                _lastUpdated = now;
            }
            else if (_lastUpdated > now)
            {
                _lastUpdated = -1;
            }

            var assignments = new List<Assignment>();
            lock (_assignmentsLock)
            {
                assignments = _assignments;
            }
            foreach (var assignment in assignments)
            {
                if (assignment.Enabled && assignment.Time != 0 && now >= assignment.Time && _lastUpdated < assignment.Time)
                {
                    await TriggerAssignment(assignment, string.Empty);
                }
            }
            _lastUpdated = now;
        }

        private async Task TriggerAssignment(Assignment assignment, string instanceName, bool force = false)
        {
            if (!(force || assignment.Enabled && (assignment.Date == null || assignment.Date == DateTime.UtcNow)))
                return;

            var devices = await GetDevicesAsync(assignment);
            if (devices.Count == 0)
            {
                _logger.LogWarning($"Failed to trigger assignment {assignment.Id}, unable to find devices");
                return;
            }

            var devicesToUpdate = new List<Device>();
            //using (var context = _factory.CreateDbContext())
            {
                foreach (var device in devices)
                {
                    if (force ||
                        (string.IsNullOrEmpty(instanceName) || string.Compare(device.InstanceName, instanceName, true) == 0) &&
                        string.Compare(device.InstanceName, assignment.InstanceName, true) != 0 &&
                        (string.IsNullOrEmpty(assignment.SourceInstanceName) || string.Compare(assignment.SourceInstanceName, device.InstanceName, true) == 0)

                    )
                    {
                        _logger.LogInformation($"Assigning device {device.Uuid} to {assignment.InstanceName}");

                        device.InstanceName = assignment.InstanceName;
                        devicesToUpdate.Add(device);
                    }
                }
            }

            // Save/update all device's new assigned instance at once
            await SaveDevicesAsync(devicesToUpdate);

            // Reload all triggered devices
            foreach (var device in devicesToUpdate)
            {
                _jobControllerService.ReloadDevice(device, device.Uuid);
            }
        }

        private async Task<List<Device>> GetDevicesAsync(Assignment assignment)
        {
            var devices = new List<Device>();
            try
            {
                using (var context = _factory.CreateDbContext())
                {
                    // If assignment assigned to device, add to devices list
                    if (!string.IsNullOrEmpty(assignment.DeviceUuid))
                    {
                        var device = await context.Devices.FindAsync(assignment.DeviceUuid);
                        if (device != null)
                        {
                            devices.Add(device);
                        }
                    }
                    // If assignment assigned to device group, add all devices to devices list
                    if (!string.IsNullOrEmpty(assignment.DeviceGroupName))
                    {
                        /*
                        // TODO: Device Groups
                        var deviceGroup = await context.DeviceGroups.FindAsync(assignment.DeviceGroupName);
                        if (deviceGroup?.Devices?.Count > 0)
                        {
                            var devicesInGroup = await context.DeviceGroups.FindAsync(deviceGroup.Devices);
                            if (devicesInGroup?.Count > 0)
                            {
                                devices.AddRange(devicesInGroup);
                            }
                        }
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            return devices;
        }

        private async Task SaveDevicesAsync(List<Device> devices)
        {
            using (var context = _factory.CreateDbContext())
            {
                context.UpdateRange(devices);
                await context.SaveChangesAsync();
            }
        }

        private List<Assignment> GetAssignments()
        {
            using (var context = _factory.CreateDbContext())
            {
                var assignments = context.Assignments.ToList();
                return assignments;
            }
        }

        #endregion
    }
}
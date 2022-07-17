namespace ChuckDeviceConfigurator.Services.Assignments
{
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class AssignmentControllerService : IAssignmentControllerService
    {
        #region Variables

        private readonly IDbContextFactory<DeviceControllerContext> _factory;
        private readonly ILogger<IAssignmentControllerService> _logger;

        private readonly object _assignmentsLock = new();
        private readonly System.Timers.Timer _timer;
        private List<Assignment> _assignments;
        private bool _initialized;
        private long _lastUpdated;

        #endregion

        #region Events

        public event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;
        private void OnDeviceReloaded(Device device)
        {
            DeviceReloaded?.Invoke(this, new AssignmentDeviceReloadedEventArgs(device));
        }

        #endregion

        #region Constructor

        public AssignmentControllerService(
            ILogger<IAssignmentControllerService> logger,
            IDbContextFactory<DeviceControllerContext> factory)
        {
            _logger = logger;
            _factory = factory;

            _lastUpdated = -2;
            _assignments = new List<Assignment>();
            _timer = new System.Timers.Timer(5 * 1000); // 5 second interval
            _timer.Elapsed += async (sender, e) => await CheckAssignmentsAsync();
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

        public void Add(Assignment assignment)
        {
            lock (_assignmentsLock)
            {
                _assignments.Add(assignment);
            }
        }

        public void Edit(Assignment newAssignment, uint oldAssignmentId)
        {
            Delete(oldAssignmentId);
            Add(newAssignment);
        }

        public void Delete(Assignment assignment)
        {
            Delete(assignment.Id);
        }

        public void Delete(uint id)
        {
            lock (_assignmentsLock)
            {
                _assignments = _assignments.Where(x => x.Id != id)
                                           .ToList();
            }
        }

        public Assignment GetByName(uint name)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<Assignment> GetByNames(IReadOnlyList<uint> names)
        {
            throw new NotImplementedException();
        }

        public async Task InstanceControllerComplete(string name)
        {
            foreach (var assignment in _assignments)
            {
                // Only trigger enabled on-complete assignments
                if (assignment.Enabled && assignment.Time == 0)
                {
                    await TriggerAssignmentAsync(assignment, name);
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task CheckAssignmentsAsync()
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
                    await TriggerAssignmentAsync(assignment, string.Empty);
                }
            }
            _lastUpdated = now;
        }

        private async Task TriggerAssignmentAsync(Assignment assignment, string instanceName, bool force = false)
        {
            if (!(force || assignment.Enabled && (assignment.Date == null || assignment.Date == default || assignment.Date == DateTime.UtcNow)))
                return;

            var devices = await GetDevicesAsync(assignment);
            if (devices.Count == 0)
            {
                _logger.LogWarning($"Failed to trigger assignment {assignment.Id}, unable to find devices");
                return;
            }

            var devicesToUpdate = new List<Device>();
            foreach (var device in devices)
            {
                if (force ||
                    // Check if provided instance is null or if device is assigned to provided instance
                    (string.IsNullOrEmpty(instanceName) || string.Compare(device.InstanceName, instanceName, true) == 0) &&
                    // Check that the device isn't already assigned to the assignment instance
                    string.Compare(device.InstanceName, assignment.InstanceName, true) != 0 &&
                    // Check if the specified source assignment instance is null or if the device is switching from the desired source instance
                    (string.IsNullOrEmpty(assignment.SourceInstanceName) || string.Compare(assignment.SourceInstanceName, device.InstanceName, true) == 0)
                )
                {
                    _logger.LogInformation($"Assigning device {device.Uuid} to {assignment.InstanceName}");

                    device.InstanceName = assignment.InstanceName;
                    devicesToUpdate.Add(device);
                }
            }

            // Save/update all device's new assigned instance at once.
            await SaveDevicesAsync(devicesToUpdate);

            // Reload all triggered devices.
            foreach (var device in devicesToUpdate)
            {
                _logger.LogDebug($"Reloading device: {device.Uuid}");
                OnDeviceReloaded(device);
            }
        }

        private async Task<List<Device>> GetDevicesAsync(Assignment assignment)
        {
            var devices = new List<Device>();
            try
            {
                using (var context = _factory.CreateDbContext())
                {
                    // If assignment assigned to device, pull from database and add to devices list.
                    if (!string.IsNullOrEmpty(assignment.DeviceUuid))
                    {
                        var device = await context.Devices.FindAsync(assignment.DeviceUuid);
                        if (device != null)
                        {
                            devices.Add(device);
                        }
                    }

                    // If assignment assigned to device group, pull from database and add all devices
                    // to devices list.
                    if (!string.IsNullOrEmpty(assignment.DeviceGroupName))
                    {
                        // Get device group from database.
                        var deviceGroup = await context.DeviceGroups.FindAsync(assignment.DeviceGroupName);
                        // Redundant check since device groups are required to have at least one device,
                        // but better safe than sorry.
                        if (deviceGroup?.Devices?.Count > 0)
                        {
                            // Get device entities from uuids.
                            var devicesInGroup = context.Devices.Where(d => deviceGroup.Devices.Contains(d.Uuid))
                                                                .ToList();
                            if (devicesInGroup != null && devicesInGroup?.Count > 0)
                            {
                                devices.AddRange(devicesInGroup);
                            }
                        }
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
                // TODO: Use EfCore.BulkExtensions BulkChangesAsync method
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

    public sealed class AssignmentDeviceReloadedEventArgs : EventArgs
    {
        public Device Device { get; }

        public AssignmentDeviceReloadedEventArgs(Device device)
        {
            Device = device;
        }
    }
}
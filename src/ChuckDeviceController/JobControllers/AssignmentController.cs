namespace ChuckDeviceController.JobControllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public interface IAssignmentController
    {
        Task Start();

        Task Stop();

        void AddAssignment(Assignment assignment);

        void EditAssignment(uint oldAssignmentId, Assignment newAssignment);

        void DeleteAssignment(Assignment assignment);

        Task TriggerAssignment(Assignment assignment, string instanceName, bool force = false);

        Task InstanceControllerDone(string name);
    }

    public class AssignmentController : IAssignmentController
    {
        private readonly IServiceScopeFactory _servicesFactory;
        private readonly IDbContextFactory<DeviceControllerContext> _dbContextFactory;
        //private readonly IInstanceController _instanceController;
        private readonly ILogger<AssignmentController> _logger;

        private readonly AssignmentRepository _assignmentRepository;
        private readonly DeviceRepository _deviceRepository;
        private readonly DeviceGroupRepository _deviceGroupRepository;
        private List<Assignment> _assignments;
        private bool _initialized;
        private long _lastUpdated;
        private readonly System.Timers.Timer _timer;
        private readonly object _assignmentsLock = new();

        #region Singleton

        private static AssignmentController _instance;
        public static AssignmentController Instance
        {
            get
            {
                return _instance ??= new AssignmentController();
            }
        }

        #endregion

        public AssignmentController(
            //IInstanceController instanceController,
            IServiceScopeFactory servicesFactory,
            IDbContextFactory<DeviceControllerContext> dbContextFactory,
            ILogger<AssignmentController> logger)
        {
            _assignments = new List<Assignment>();
            _initialized = false;
            _lastUpdated = -2;

            _servicesFactory = servicesFactory;
            //_instanceController = instanceController;
            _dbContextFactory = dbContextFactory;
            _logger = logger;

            //_assignmentRepository = new AssignmentRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            //_deviceRepository = new DeviceRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            //_deviceGroupRepository = new DeviceGroupRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _assignmentRepository = new AssignmentRepository(_dbContextFactory.CreateDbContext());
            _deviceRepository = new DeviceRepository(_dbContextFactory.CreateDbContext());
            _deviceGroupRepository = new DeviceGroupRepository(_dbContextFactory.CreateDbContext());

            _timer = new System.Timers.Timer
            {
                Interval = 5000
            };
            _timer.Elapsed += async (sender, e) => await CheckAssignments().ConfigureAwait(false);

            Start().ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
        }

        public async Task Start()
        {
            lock (_assignmentsLock)
            {
                _assignments = (List<Assignment>)_assignmentRepository.GetAllAsync()
                                                                      .ConfigureAwait(false)
                                                                      .GetAwaiter()
                                                                      .GetResult();
            }
            if (!_initialized)
            {
                _logger.LogInformation("Starting AssignmentController");
                _initialized = true;
                _timer.Start();
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task Stop()
        {
            _timer?.Stop();
            await Task.CompletedTask;
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
            lock (_assignmentsLock)
            {
                _assignments.Remove(assignment);
            }
        }

        public void DeleteAssignment(uint id)
        {
            lock (_assignmentsLock)
            {
                var assignment = _assignments.Find(x => x.Id == id);
                if (assignment != null)
                {
                    DeleteAssignment(assignment);
                }
            }
        }

        public async Task TriggerAssignment(Assignment assignment, string instance, bool force = false)
        {
            if (!(force || (assignment.Enabled && (assignment.Date == default || assignment.Date == DateTime.UtcNow))))
                return;

            var devices = new List<Device>();
            try
            {
                // If assignment assigned to device, add to devices list
                if (!string.IsNullOrEmpty(assignment.DeviceUuid))
                {
                    var device = await _deviceRepository.GetByIdAsync(assignment.DeviceUuid).ConfigureAwait(false);
                    devices.Add(device);
                }
                // If assignment assigned to device group, add all devices to devices list
                if (!string.IsNullOrEmpty(assignment.DeviceGroupName))
                {
                    var deviceGroup = await _deviceGroupRepository.GetByIdAsync(assignment.DeviceGroupName).ConfigureAwait(false);
                    if (deviceGroup?.Devices?.Count > 0)
                    {
                        var devicesInGroup = await _deviceRepository.GetByIdsAsync(deviceGroup.Devices).ConfigureAwait(false);
                        if (devicesInGroup?.Count > 0)
                        {
                            devices.AddRange(devicesInGroup);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            if (devices?.Count == 0)
            {
                _logger.LogWarning($"Failed to trigger assignment {assignment.Id}, unable to find devices");
                return;
            }
            using (var scope = _servicesFactory.CreateScope())
            {
                var instanceController = scope.ServiceProvider.GetRequiredService<IInstanceController>();
                if (instanceController == null)
                {
                    _logger.LogError($"Failed to get InstanceController service from DI");
                    return;
                }
                foreach (var device in devices)
                {
                    if (force || (
                        (string.IsNullOrEmpty(instance) || string.Compare(device.InstanceName, instance, true) == 0) &&
                        string.Compare(device.InstanceName, assignment.InstanceName, true) != 0 &&
                        (string.IsNullOrEmpty(assignment.SourceInstanceName) || string.Compare(assignment.SourceInstanceName, device.InstanceName, true) == 0)
                        )
                    )
                    {
                        _logger.LogInformation($"Assigning device {device.Uuid} to {assignment.InstanceName}");
                        await instanceController.RemoveDevice(device).ConfigureAwait(false);
                        device.InstanceName = assignment.InstanceName;
                        await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
                        instanceController.AddDevice(device);
                    }
                }
            }
        }

        public async Task InstanceControllerDone(string name)
        {
            foreach (var assignment in _assignments)
            {
                if (assignment.Enabled && assignment.Time == 0)
                {
                    await TriggerAssignment(assignment, name).ConfigureAwait(false);
                }
            }
        }

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
            List<Assignment> assignments;
            lock (_assignmentsLock)
            {
                assignments = _assignments;
            }
            foreach (var assignment in assignments)
            {
                if (assignment.Enabled && assignment.Time != 0 && now > assignment.Time && _lastUpdated < assignment.Time)
                {
                    await TriggerAssignment(assignment, string.Empty).ConfigureAwait(false);
                }
            }
            _lastUpdated = now;
        }
    }
}
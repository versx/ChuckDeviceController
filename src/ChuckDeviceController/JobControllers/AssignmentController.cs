namespace ChuckDeviceController.JobControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;
    using Chuck.Infrastructure.Data.Repositories;
    using Chuck.Infrastructure.Extensions;

    public class AssignmentController
    {
        private readonly ILogger<AssignmentController> _logger;

        private readonly AssignmentRepository _assignmentRepository;
        private readonly DeviceRepository _deviceRepository;
        private List<Assignment> _assignments;
        // TODO: private readonly string _timeZone;
        private bool _initialized;
        private long _lastUpdated;

        private readonly object _assignmentsLock = new object();

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

        public AssignmentController()
        {
            _assignments = new List<Assignment>();
            //_timeZone = null; //config.timezone;
            _initialized = false;
            _lastUpdated = -2;

            _logger = new Logger<AssignmentController>(LoggerFactory.Create(x => x.AddConsole()));

            _assignmentRepository = new AssignmentRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _deviceRepository = new DeviceRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));

            Initialize().ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
        }

        public async Task Initialize()
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
                var timer = new System.Timers.Timer
                {
                    Interval = 5000
                };
                timer.Elapsed += async (sender, e) => await CheckAssignments().ConfigureAwait(false);
                timer.Start();
            }
            await Task.CompletedTask.ConfigureAwait(false);
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

        public async Task TriggerAssignment(Assignment assignment, bool force = false)
        {
            if (!(force || (assignment.Enabled && (assignment.Date == default || assignment.Date == DateTime.UtcNow))))
                return;

            Device device = null;
            try
            {
                device = await _deviceRepository.GetByIdAsync(assignment.DeviceUuid).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            if (device != null && string.Compare(device.InstanceName, assignment.InstanceName, true) != 0)
            {
                _logger.LogInformation($"Assigning device {device.Uuid} to {assignment.InstanceName}");
                await InstanceController.Instance.RemoveDevice(device).ConfigureAwait(false);
                device.InstanceName = assignment.InstanceName;
                await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
                InstanceController.Instance.AddDevice(device);
            }
        }

        public async Task InstanceControllerDone(string name)
        {
            foreach (var assignment in _assignments)
            {
                var deviceUUIDs = InstanceController.Instance.GetDeviceUuidsInInstance(name);
                if (assignment.Enabled && assignment.Time != 0 && deviceUUIDs.Contains(assignment.DeviceUuid))
                {
                    await TriggerAssignment(assignment).ConfigureAwait(false);
                }
            }
        }

        private async Task CheckAssignments()
        {
            var now = (long)DateTime.UtcNow.ToTotalSeconds();
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
                    await TriggerAssignment(assignment).ConfigureAwait(false);
                }
            }
            _lastUpdated = now;
        }
    }
}
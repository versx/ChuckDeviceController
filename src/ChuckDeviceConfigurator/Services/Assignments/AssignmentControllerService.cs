namespace ChuckDeviceConfigurator.Services.Assignments;

using System.Collections.Generic;

using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
using ChuckDeviceConfigurator.Services.Geofences;
using ChuckDeviceController.Collections;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories.Dapper;

public class AssignmentControllerService : IAssignmentControllerService
{
    private const ushort CheckAssignmentsIntervalS = 5;

    #region Variables

    private readonly ILogger<IAssignmentControllerService> _logger;
    private readonly IDapperUnitOfWork _uow;
    private readonly IGeofenceControllerService _geofenceService;

    private readonly System.Timers.Timer _timer;
    private SafeCollection<Assignment> _assignments;
    private bool _initialized;
    private long _lastUpdated;

    #endregion

    #region Events

    public event EventHandler<AssignmentDeviceReloadedEventArgs>? DeviceReloaded;
    private void OnDeviceReloaded(Device device)
    {
        DeviceReloaded?.Invoke(this, new AssignmentDeviceReloadedEventArgs(device));
    }

    public event EventHandler<ReloadInstanceEventArgs>? ReloadInstance;
    private void OnReloadInstance(Instance instance)
    {
        ReloadInstance?.Invoke(this, new ReloadInstanceEventArgs(instance));
    }

    #endregion

    #region Constructor

    public AssignmentControllerService(
        ILogger<IAssignmentControllerService> logger,
        IDapperUnitOfWork uow,
        IGeofenceControllerService geofenceService)
    {
        _logger = logger;
        _uow = uow;

        _lastUpdated = -2;
        _assignments = new SafeCollection<Assignment>();
        _timer = new System.Timers.Timer(CheckAssignmentsIntervalS * 1000);
        _timer.Elapsed += async (sender, e) => await CheckAssignmentsAsync();
        _geofenceService = geofenceService;
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
        var assignments = GetAssignmentsAsync().Result;
        _assignments = new(assignments);
    }

    public void Add(Assignment assignment)
    {
        if (_assignments.Contains(assignment))
        {
            // Already exists
            return;
        }
        if (!_assignments.TryAdd(assignment))
        {
            _logger.LogError($"Failed to add assignment with id '{assignment.Id}'");
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
        if (!_assignments.Remove(x => x.Id == id))
        {
            _logger.LogError($"Failed to remove assignment with id '{id}'");
        }
    }

    public Assignment GetByName(uint id)
    {
        var assignment = _assignments.Get(x => x.Id == id);
        return assignment;
    }

    public IReadOnlyList<Assignment> GetByNames(IReadOnlyList<uint> names)
    {
        var assignments = names
            .Select(GetByName)
            .ToList();
        return assignments;
    }

    #region Start Assignments

    public async Task StartAssignmentAsync(Assignment assignment)
    {
        await TriggerAssignmentAsync(assignment, force: true);
    }

    public async Task StartAssignmentGroupAsync(AssignmentGroup assignmentGroup)
    {
        var assignmentIds = assignmentGroup.AssignmentIds;
        var assignments = _assignments
            .Where(assignment => assignmentIds.Contains(assignment.Id))
            .ToList();

        foreach (var assignment in assignments)
        {
            await StartAssignmentAsync(assignment);
        }
    }

    #endregion

    #region ReQuest Assignments

    public async Task ReQuestAssignmentAsync(uint assignmentId)
    {
        await ReQuestAssignmentsAsync(new[] { assignmentId });
    }

    public async Task ReQuestAssignmentsAsync(IEnumerable<uint> assignmentIds)
    {
        var assignments = assignmentIds
            .Select(id => _assignments.FirstOrDefault(x => x.Id == id))
            .Where(assignment => assignment != null)
            .Where(assignment => assignment?.Enabled ?? false)
            .ToList();

        var instances = await _uow.Instances.FindAsync(instance => instance.Type == InstanceType.AutoQuest);
        var geofenceNames = instances
            .SelectMany(x => x.Geofences)
            .ToList();
        var geofences = _geofenceService.GetByNames(geofenceNames);
        var instancesToClear = new List<Instance>();
        foreach (var assignment in assignments)
        {
            var affectedInstanceNames = ResolveAssignmentChain(assignment!);
            var affectedInstances = instances
                .Where(x => affectedInstanceNames.Contains(x.Name))
                .ToList();
            var affectedInstanceNotAdded = affectedInstances.Where(x => !instancesToClear.Contains(x));
            foreach (var instance in affectedInstanceNotAdded)
            {
                instancesToClear.Add(instance);
            }
        }
        _logger.LogInformation($"Re-Quest will clear quests for {instancesToClear.Count:N0} instances");

        foreach (var instance in instancesToClear)
        {
            var instanceGeofences = geofences
                .Where(geofence => instance.Geofences.Contains(geofence.Name))
                .ToList();
            if (instanceGeofences?.Any() ?? false)
            {
                _logger.LogInformation($"Clearing quests for geofences: {string.Join(", ", instanceGeofences.Select(x => x.Name))}");
                await _uow.ClearQuestsAsync(instanceGeofences);
            }

            // Trigger event to reload quest instances
            OnReloadInstance(instance);
        }

        // Trigger assignments
        foreach (var assignment in assignments)
        {
            if (assignment == null)
                continue;

            await TriggerAssignmentAsync(assignment, force: true);
        }
    }

    #endregion

    #region Clear Quests

    public async Task ClearQuestsAsync(Assignment assignment)
    {
        // Clear quests for assignment
        await ClearQuestsAsync(new[] { assignment });
    }

    public async Task ClearQuestsAsync(IEnumerable<uint> assignmentIds)
    {
        // Clear quests for assignments
        var assignments = (await GetAssignmentsAsync())
            .Where(x => assignmentIds.Contains(x.Id));
        await ClearQuestsAsync(assignments);
    }

    public async Task ClearQuestsAsync(IEnumerable<Assignment> assignments)
    {
        // Clear quests for assignments
        foreach (var assignment in assignments)
        {
            var instance = await _uow.Instances.FindAsync(assignment.InstanceName);
            if (instance == null)
            {
                // Failed to retrieve instance from database, does it exist?
                _logger.LogError($"Assignment instance does not exist with name '{assignment.InstanceName}'.");
                return;
            }

            var geofences = _geofenceService.GetByNames(instance.Geofences);
            if (!(geofences?.Any() ?? false))
            {
                // Failed to retrieve assignment from database, does it exist?
                _logger.LogError($"Failed to retrieve geofence(s) ('{string.Join(", ", instance.Geofences)}') for assignment instance '{instance.Name}'.");
                return;
            }

            // Clear quests for all geofences assigned to instance
            var geofenceNames = geofences.Select(x => x.Name);
            _logger.LogInformation($"Clearing quests for geofences '{string.Join(", ", geofenceNames)}'");
            await _uow.ClearQuestsAsync(geofences);

            _logger.LogInformation($"All quests have been cleared for assignment '{assignment.Id}' (Instance: {instance.Name}, Geofences: {string.Join(", ", instance.Geofences)})");
        }
    }

    #endregion

    public async Task InstanceControllerCompleteAsync(string instanceName)
    {
        // Only trigger enabled on-complete assignments
        var assignments = _assignments
             .Where(x => x.Enabled && x.Time == 0)
             .ToList();
        foreach (var assignment in assignments)
        {
            await TriggerAssignmentAsync(assignment, instanceName);
        }
    }

    #endregion

    #region Private Methods

    private List<string> ResolveAssignmentChain(Assignment assignment)
    {
        var result = new List<Assignment>();
        var assignments = _assignments.Where(a => a.Enabled).ToList();
        var toVisit = new List<Assignment> { assignment };
        while (toVisit.Count > 0)
        {
            var found = false;
            for (var i = 0; i < toVisit.Count; i++)
            {
                var source = toVisit[i];
                var chained = assignments.Where(a => a.SourceInstanceName == source.InstanceName);
                foreach (var target in chained)
                {
                    if (!toVisit.Contains(target))
                    {
                        toVisit.Add(target);
                    }
                }
                if (!result.Contains(source))
                {
                    found = true;
                    result.Add(source);
                }
                var index = toVisit.IndexOf(source);
                if (index > -1)
                {
                    toVisit.RemoveAt(index);
                }
            }
            if (!found) break;
        }
        var instanceNames = result.Select(a => a.InstanceName).ToList();
        return instanceNames;
    }

    private async Task CheckAssignmentsAsync()
    {
        var dateNow = DateTime.Now;
        var now = dateNow.Hour * Strings.SixtyMinutesS + dateNow.Minute * 60 + dateNow.Second;
        if (_lastUpdated == -2)
        {
            _lastUpdated = now;
        }
        else if (_lastUpdated > now)
        {
            _lastUpdated = -1;
        }

        foreach (var assignment in _assignments)
        {
            if (assignment.Enabled && assignment.Time != 0 && now >= assignment.Time && _lastUpdated < assignment.Time)
            {
                await TriggerAssignmentAsync(assignment);
            }
        }
        _lastUpdated = now;
    }

    private async Task TriggerAssignmentAsync(Assignment assignment, string? instanceName = null, bool force = false)
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
        var result = await _uow.Devices.UpdateRangeAsync(devicesToUpdate, mappings: new Dictionary<string, Func<Device, object?>>
        {
            ["uuid"] = x => x.Uuid,
            ["instance_name"] = x => x.InstanceName,
        });
        if (result <= 0)
        {
            // Failed to update devices
            _logger.LogError($"Failed to update devices assigned instance");
        }

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
            // If assignment assigned to device, pull from database and add to devices list.
            if (!string.IsNullOrEmpty(assignment.DeviceUuid))
            {
                var device = await _uow.Devices.FindAsync(assignment.DeviceUuid);
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
                var deviceGroup = await _uow.DeviceGroups.FindAsync(assignment.DeviceGroupName);
                if (deviceGroup == null)
                {
                    _logger.LogError($"Failed to find device group by name '{assignment.DeviceGroupName}' to retrieve device list for assignment '{assignment.Id}'");
                    return null;
                }

                // Redundant check since device groups are required to have at least one device,
                // but better safe than sorry.
                if ((deviceGroup?.DeviceUuids?.Count ?? 0) > 0)
                {
                    // Get device entities from uuids.
                    var devicesInGroup = await _uow.Devices.FindAsync(d => deviceGroup!.DeviceUuids.Contains(d.Uuid));
                    if (devicesInGroup?.Any() ?? false)
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
        return devices;
    }

    private async Task<IEnumerable<Assignment>> GetAssignmentsAsync()
    {
        var assignments = await _uow.Assignments.FindAllAsync();
        return assignments;
    }

    #endregion
}
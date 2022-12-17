namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;

    using ChuckDeviceConfigurator.Services.Assignments;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;

    [Authorize(Roles = RoleConsts.AssignmentsRole)]
    public class AssignmentController : Controller
    {
        private readonly ILogger<AssignmentController> _logger;
        private readonly IUnitOfWork _uow;
        private readonly IAssignmentControllerService _assignmentService;

        public AssignmentController(
            ILogger<AssignmentController> logger,
            IUnitOfWork uow,
            IAssignmentControllerService assignmentService)
        {
            _logger = logger;
            _uow = uow;
            _assignmentService = assignmentService;
        }

        // GET: AssignmentController
        public async Task<ActionResult> Index()
        {
            var assignments = await _uow.Assignments.FindAllAsync();
            // Include instance type for removing Re-Quest button
            var questInstances = (await _uow.Instances.FindAsync(x => x.Type == InstanceType.AutoQuest))
                .Select(x => x.Name)
                .ToList();
            ViewBag.QuestInstances = questInstances;
            return View(new ViewModelsModel<Assignment>
            {
                Items = assignments.ToList(),
            });
        }

        // GET: AssignmentController/Details/5
        public async Task<ActionResult> Details(uint id)
        {
            var assignment = await _uow.Assignments.FindByIdAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }
            var instance = await _uow.Instances.FindByIdAsync(assignment.InstanceName);
            if (instance == null)
            {
                _logger.LogWarning($"Failed to retrieve instance with name '{assignment.InstanceName}' for assignment '{id}'.");
            }
            ViewBag.IsQuest = (instance?.Type ?? InstanceType.CirclePokemon) == InstanceType.AutoQuest;
            return View(assignment);
        }

        // GET: AssignmentController/Create
        public async Task<ActionResult> Create()
        {
            var instances = await _uow.Instances.FindAllAsync();
            var devicesAndDeviceGroups = await BuildDevicesSelectListAsync();

            ViewBag.Devices = devicesAndDeviceGroups;
            ViewBag.Instances = instances;
            return View();
        }

        // POST: AssignmentController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var deviceOrDeviceGroupName = Convert.ToString(collection["DeviceUuid"]);
                var uuid = deviceOrDeviceGroupName.StartsWith("device:")
                    ? new string(deviceOrDeviceGroupName.Skip(7).ToArray())
                    : null;
                var deviceGroupName = deviceOrDeviceGroupName.StartsWith("group:")
                    ? new string(deviceOrDeviceGroupName.Skip(6).ToArray())
                    : null;

                var sourceInstanceName = Convert.ToString(collection["SourceInstanceName"]);
                var instanceName = Convert.ToString(collection["InstanceName"]);
                var date = Convert.ToString(collection["Date"]);
                DateTime? realDate = string.IsNullOrEmpty(date)
                    ? null
                    : DateTime.Parse(date);
                var time = Convert.ToString(collection["Time"]);
                var timeValue = GetTimeNumeric(time);
                var createOnComplete = collection["OnComplete"].Contains("true");
                var enabled = collection["Enabled"].Contains("true");

                if (_uow.Assignments.Any(a =>
                    a.DeviceUuid == uuid &&
                    a.DeviceGroupName == deviceGroupName &&
                    a.InstanceName == instanceName &&
                    a.SourceInstanceName == sourceInstanceName &&
                    a.Date == realDate &&
                    a.Time == timeValue &&
                    a.Enabled == enabled
                ))
                {
                    // Assignment exists already with provided details
                    ModelState.AddModelError("Assignment", $"Assignment already exists for device with provided details.");
                    return View();
                }

                var assignment = new Assignment
                {
                    DeviceUuid = uuid,
                    DeviceGroupName = deviceGroupName,
                    SourceInstanceName = sourceInstanceName ?? null,
                    InstanceName = instanceName,
                    Date = realDate,
                    Time = timeValue,
                    Enabled = enabled,
                };
                await _uow.Assignments.AddAsync(assignment);

                if (createOnComplete)
                {
                    // Create on complete assignment with the same properties
                    var completeAssignment = new Assignment
                    {
                        DeviceUuid = uuid,
                        DeviceGroupName = deviceGroupName,
                        SourceInstanceName = sourceInstanceName ?? null,
                        InstanceName = instanceName,
                        Date = realDate,
                        Time = 0,
                        Enabled = enabled,
                    };
                    await _uow.Assignments.AddAsync(completeAssignment);
                }

                await _uow.CommitAsync();

                _assignmentService.Add(assignment);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while creating new assignment.");
                return View();
            }
        }

        // GET: AssignmentController/Edit/5
        public async Task<ActionResult> Edit(uint id)
        {
            var assignment = await _uow.Assignments.FindByIdAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }

            var devices = await BuildDevicesSelectListAsync(assignment);
            var instances = await _uow.Instances.FindAllAsync();

            ViewBag.Devices = devices;
            ViewBag.Instances = instances;
            return View(assignment);
        }

        // POST: AssignmentController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(uint id, IFormCollection collection)
        {
            try
            {
                var assignment = await _uow.Assignments.FindByIdAsync(id);
                if (assignment == null)
                {
                    // Failed to retrieve assignment from database, does it exist?
                    ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                    return View();
                }

                var deviceOrDeviceGroupName = Convert.ToString(collection["DeviceUuid"]);
                var uuid = deviceOrDeviceGroupName.StartsWith("device:")
                    ? new string(deviceOrDeviceGroupName.Skip(7).ToArray())
                    : null;
                var deviceGroupName = deviceOrDeviceGroupName.StartsWith("group:")
                    ? new string(deviceOrDeviceGroupName.Skip(6).ToArray())
                    : null;

                var sourceInstanceName = Convert.ToString(collection["SourceInstanceName"]);
                var instanceName = Convert.ToString(collection["InstanceName"]);
                var date = Convert.ToString(collection["Date"]);
                DateTime? realDate = string.IsNullOrEmpty(date)
                    ? null
                    : DateTime.Parse(date);
                var time = Convert.ToString(collection["Time"]);
                var timeValue = GetTimeNumeric(time);
                var enabled = collection["Enabled"].Contains("on");

                if (_uow.Assignments.Any(a =>
                    a.DeviceUuid == uuid &&
                    a.DeviceGroupName == deviceGroupName &&
                    a.InstanceName == instanceName &&
                    a.SourceInstanceName == sourceInstanceName &&
                    a.Date == realDate &&
                    a.Time == timeValue &&
                    a.Enabled == enabled
                ))
                {
                    // Assignment exists already with provided details
                    ModelState.AddModelError("Assignment", $"Assignment already exists for device with provided details.");
                    return View();
                }

                assignment.SourceInstanceName = sourceInstanceName;
                assignment.InstanceName = instanceName;
                assignment.DeviceUuid = uuid;
                assignment.DeviceGroupName = deviceGroupName;
                assignment.Date = realDate;
                assignment.Time = timeValue;
                assignment.Enabled = enabled;

                await _uow.Assignments.UpdateAsync(assignment);
                await _uow.CommitAsync();

                _assignmentService.Edit(assignment, id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while editing assignment '{id}'.");
                return View();
            }
        }

        // GET: AssignmentController/Delete/5
        public async Task<ActionResult> Delete(uint id)
        {
            var assignment = await _uow.Assignments.FindByIdAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }
            return View(assignment);
        }

        // POST: AssignmentController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(uint id, IFormCollection collection)
        {
            try
            {
                var assignment = await _uow.Assignments.FindByIdAsync(id);
                if (assignment == null)
                {
                    // Failed to retrieve geofence from database, does it exist?
                    ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                    return View(assignment);
                }

                // Delete assignment from database
                await _uow.Assignments.RemoveAsync(assignment);
                await _uow.CommitAsync();

                _assignmentService.Delete(assignment);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while deleting assignment '{id}'.");
                return View();
            }
        }

        // GET: AssignmentController/DeleteAll
        [HttpGet]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                var assignments = await _uow.Assignments.FindAllAsync();
                _uow.Assignments.RemoveRange(assignments);
                await _uow.CommitAsync();

                // Reload assignments in the assignment service to reflect the changes.
                _assignmentService.Reload();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while deleting all assignments.");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: AssignmentController/Start/5
        public async Task<ActionResult> Start(uint id)
        {
            try
            {
                var assignment = await _uow.Assignments.FindByIdAsync(id);
                if (assignment == null)
                {
                    // Failed to retrieve assignment from database, does it exist?
                    ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                    return View(assignment);
                }

                // Start device assignment
                await _assignmentService.StartAssignmentAsync(assignment);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while starting assignment '{id}'.");
                return View();
            }
        }

        // GET: AssignmentController/ReQuest/test
        public async Task<ActionResult> ReQuest(uint id)
        {
            try
            {
                var assignment = await _uow.Assignments.FindByIdAsync(id);
                if (assignment == null)
                {
                    // Failed to retrieve assignment from database, does it exist?
                    ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                    return View();
                }

                // Start re-quest for assignment
                await _assignmentService.ReQuestAssignmentAsync(assignment.Id);

                return RedirectToAction(nameof(Index));
            }
            catch //(Exception ex)
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while starting re-quest for assignment {id}.");
                return View();
            }
        }

        // GET: AssignmentController/ClearQuests/test
        public async Task<ActionResult> ClearQuests(uint id)
        {
            // Clear quests for assignment
            var assignment = await _uow.Assignments.FindByIdAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }

            await _assignmentService.ClearQuestsAsync(assignment);

            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods

        private uint GetTimeNumeric(string time)
        {
            var value = 0u;
            if (string.IsNullOrEmpty(time))
            {
                return value;
            }

            var split = time.Split(':').ToList();
            if (split.Count == 2)
            {
                split.Add("00");
            }
            if (split.Count == 3)
            {
                var hours = Convert.ToUInt32(split[0]);
                var minutes = Convert.ToUInt32(split[1]);
                var seconds = Convert.ToUInt32(split[2]);
                var timeValue = hours * Strings.SixtyMinutesS + minutes * 60 + seconds;
                value = timeValue;// == 0 ? 1 : timeValue;
            }
            else
            {
                ModelState.AddModelError("Assingment", "Invalid time provided");
            }
            return value;
        }

        private async Task<List<SelectListItem>> BuildDevicesSelectListAsync(Assignment? assignment = null)
        {
            var devices = await _uow.Devices.FindAllAsync();
            var deviceGroups = await _uow.DeviceGroups.FindAllAsync();
            var selectedDevice = assignment?.DeviceUuid;
            var selectedDeviceGroup = assignment?.DeviceGroupName;

            var devicesAndDeviceGroups = new List<SelectListItem>();
            var devicesGroup = new SelectListGroup { Name = "Devices" };
            var deviceGroupsGroup = new SelectListGroup { Name = "Device Groups" };

            devices.ToList().ForEach(device =>
            {
                devicesAndDeviceGroups.Add(new SelectListItem
                {
                    Text = device.Uuid,
                    Value = "device:" + device.Uuid,
                    Selected = device.Uuid == selectedDevice,
                    Group = devicesGroup,
                });
            });
            deviceGroups.ToList().ForEach(deviceGroup =>
            {
                devicesAndDeviceGroups.Add(new SelectListItem
                {
                    Text = deviceGroup.Name,
                    Value = "group:" + deviceGroup.Name,
                    Selected = deviceGroup.Name == selectedDeviceGroup,
                    Group = deviceGroupsGroup,
                });
            });
            return devicesAndDeviceGroups;
        }

        #endregion
    }
}
namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;

    using ChuckDeviceConfigurator.Services.Assignments;
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.AssignmentsRole)]
    public class AssignmentController : Controller
    {
        private readonly ILogger<AssignmentController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IAssignmentControllerService _assignmentService;

        public AssignmentController(
            ILogger<AssignmentController> logger,
            DeviceControllerContext context,
            IAssignmentControllerService assignmentService)
        {
            _logger = logger;
            _context = context;
            _assignmentService = assignmentService;
        }

        // GET: AssignmentController
        public ActionResult Index(int page = 1, int pageSize = 100)
        {
            var assignments = _context.Assignments.ToList();

            var total = assignments.Count;
            var maxPage = (total / pageSize) - (total % pageSize == 0 ? 1 : 0) + 1;
            page = page > maxPage ? maxPage : page;

            var pagedAssignments = assignments.OrderBy(key => key.Id)
                                              .Skip((page - 1) * pageSize)
                                              .Take(pageSize)
                                              .ToList();

            ViewBag.MaxPage = maxPage;
            ViewBag.Page = page;
            ViewBag.NextPages = Utils.GetNextPages(page, maxPage);
            return View(new ViewModelsModel<Assignment>
            {
                Items = pagedAssignments,
            });
        }

        // GET: AssignmentController/Details/5
        public async Task<ActionResult> Details(uint id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }
            return View(assignment);
        }

        // GET: AssignmentController/Create
        public ActionResult Create()
        {
            var instances = _context.Instances.ToList();
            var devicesAndDeviceGroups = BuildDevicesSelectList();

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

                if (_context.Assignments.Any(a =>
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
                await _context.AddAsync(assignment);

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
                    await _context.AddAsync(completeAssignment);
                }

                await _context.SaveChangesAsync();

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
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
            {
                // Failed to retrieve assignment from database, does it exist?
                ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                return View();
            }

            var devices = BuildDevicesSelectList(assignment);
            var instances = _context.Instances.ToList();

            // TODO: Fix loading time value into element

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
                var assignment = await _context.Assignments.FindAsync(id);
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

                if (_context.Assignments.Any(a =>
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

                _context.Assignments.Update(assignment);
                await _context.SaveChangesAsync();

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
            var assignment = await _context.Assignments.FindAsync(id);
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
                var assignment = await _context.Assignments.FindAsync(id);
                if (assignment == null)
                {
                    // Failed to retrieve geofence from database, does it exist?
                    ModelState.AddModelError("Assignment", $"Assignment does not exist with id '{id}'.");
                    return View(assignment);
                }

                // Delete assignment from database
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();

                _assignmentService.Delete(assignment);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while deleting assignment '{id}'.");
                return View();
            }
        }

        // GET: AssignmentController/Start/5
        public async Task<ActionResult> Start(uint id)
        {
            try
            {
                var assignment = await _context.Assignments.FindAsync(id);
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

        private uint GetTimeNumeric(string time)
        {
            uint value = 0;
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
                value = timeValue == 0 ? 1 : timeValue;
            }
            else
            {
                ModelState.AddModelError("Assingment", "Invalid time provided");
            }
            return value;
        }

        private List<SelectListItem> BuildDevicesSelectList(Assignment? assignment = null)
        {
            var devices = _context.Devices.ToList();
            var deviceGroups = _context.DeviceGroups.ToList();
            var selectedDevice = assignment?.DeviceUuid;
            var selectedDeviceGroup = assignment?.DeviceGroupName;

            var devicesAndDeviceGroups = new List<SelectListItem>();
            var devicesGroup = new SelectListGroup { Name = "Devices" };
            var deviceGroupsGroup = new SelectListGroup { Name = "Device Groups" };

            devices.ForEach(device =>
            {
                devicesAndDeviceGroups.Add(new SelectListItem
                {
                    Text = device.Uuid,
                    Value = "device:" + device.Uuid,
                    Selected = device.Uuid == selectedDevice,
                    Group = devicesGroup,
                });
            });
            deviceGroups.ForEach(deviceGroup =>
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
    }
}
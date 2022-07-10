namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceConfigurator.Services.Assignments;

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
        public ActionResult Index()
        {
            var assignments = _context.Assignments.ToList();
            return View(new ViewModelsModel<Assignment>
            {
                Items = assignments,
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
            var devices = _context.Devices.ToList();
            var instances = _context.Instances.ToList();
            ViewBag.Devices = devices;
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
                var deviceUuid = Convert.ToString(collection["DeviceUuid"]);
                var sourceInstanceName = Convert.ToString(collection["SourceInstanceName"]);
                var instanceName = Convert.ToString(collection["InstanceName"]);
                var date = Convert.ToString(collection["Date"]);
                var realDate = string.IsNullOrEmpty(date) ? default : DateTime.Parse(date);
                var time = Convert.ToString(collection["Time"]);
                var createOnComplete = collection["OnComplete"].Contains("true");
                var enabled = collection["Enabled"].Contains("true");

                var timeValue = GetTimeNumeric(time);
                if (_context.Assignments.Any(a => a.DeviceUuid == deviceUuid &&
                    a.InstanceName == instanceName &&
                    a.SourceInstanceName == sourceInstanceName &&
                    a.Date == (realDate == default ? null : realDate) &&
                    a.Time == timeValue &&
                    a.Enabled == enabled))
                {
                    // Assignment exists already by name
                    ModelState.AddModelError("Assignment", $"Assignment already exists for device.");
                    return View();
                }

                var assignment = new Assignment
                {
                    DeviceUuid = deviceUuid,
                    DeviceGroupName = null,
                    SourceInstanceName = sourceInstanceName ?? null,
                    InstanceName = instanceName,
                    Date = realDate == default ? null : realDate,
                    Time = timeValue,
                    Enabled = enabled,
                };
                await _context.AddAsync(assignment);

                if (createOnComplete)
                {
                    // Create on complete assignment with the same properties
                    var completeAssignment = new Assignment
                    {
                        DeviceUuid = deviceUuid,
                        DeviceGroupName = null,
                        SourceInstanceName = sourceInstanceName ?? null,
                        InstanceName = instanceName,
                        Date = realDate == default ? null : realDate,
                        Time = 0,
                        Enabled = enabled,
                    };
                    await _context.AddAsync(completeAssignment);
                }

                await _context.SaveChangesAsync();

                _assignmentService.AddAssignment(assignment);

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

            var devices = _context.Devices.ToList();
            var instances = _context.Instances.ToList();
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

                // TODO: Device group
                var sourceInstanceName = Convert.ToString(collection["SourceInstanceName"]);
                var instanceName = Convert.ToString(collection["SourceInstanceName"]);
                var deviceUuid = Convert.ToString(collection["DeviceUuid"]);
                var date = Convert.ToString(collection["Date"]);
                var time = Convert.ToString(collection["Time"]);
                var realDate = string.IsNullOrEmpty(date) ? default : DateTime.Parse(date);
                var enabled = collection["Enabled"].Contains("on");
                var timeValue = GetTimeNumeric(time);

                assignment.SourceInstanceName = sourceInstanceName;
                assignment.InstanceName = instanceName;
                assignment.DeviceUuid = deviceUuid;
                assignment.Date = realDate;
                assignment.Time = timeValue;
                assignment.Enabled = enabled;

                _context.Assignments.Update(assignment);
                await _context.SaveChangesAsync();

                _assignmentService.EditAssignment(id, assignment);

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

                _assignmentService.DeleteAssignment(assignment);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while deleting assignment '{id}'.");
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
                var timeValue = hours * 3600 + minutes * 60 + seconds;
                value = timeValue == 0 ? 1 : timeValue;
            }
            else
            {
                ModelState.AddModelError("Assingment", "Invalid time provided");
            }
            return value;
        }
    }
}
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

    [Controller]
    [Authorize(Roles = RoleConsts.AssignmentGroupsRole)]
    public class AssignmentGroupController : Controller
    {
        private readonly ILogger<AssignmentGroupController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IAssignmentControllerService _assignmentService;

        public AssignmentGroupController(
            ILogger<AssignmentGroupController> logger,
            DeviceControllerContext context,
            IAssignmentControllerService assignmentService)
        {
            _logger = logger;
            _context = context;
            _assignmentService = assignmentService;
        }

        // GET: AssignmentGroupController
        public ActionResult Index()
        {
            var assignmentGroups = _context.AssignmentGroups.ToList();
            var model = new ViewModelsModel<AssignmentGroup>
            {
                Items = assignmentGroups,
            };
            return View(model);
        }

        // GET: AssignmentGroupController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
            if (assignmentGroup == null)
            {
                // Failed to retrieve assignment group from database, does it exist?
                ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                return View();
            }
            return View(assignmentGroup);
        }

        // GET: AssignmentGroupController/Create
        public ActionResult Create()
        {
            var assignmentsList = new List<SelectListItem>();
            var assignments = _context.Assignments.ToList();
            foreach (var assignment in assignments)
            {
                var displayText = (string.IsNullOrEmpty(assignment.DeviceGroupName)
                    ? assignment.DeviceUuid
                    : assignment.DeviceGroupName
                ) + $" -> {assignment.InstanceName} ({Utils.FormatAssignmentTime(assignment.Time)})";
                assignmentsList.Add(new SelectListItem
                {
                    Text = displayText,
                    Value = assignment.Id.ToString(),
                });
            }
            ViewBag.Assignments = assignmentsList;
            return View();
        }

        // POST: AssignmentGroupController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var assignments = collection["AssignmentIds"].ToList();
                var assignmentIds = assignments.Select(x => Convert.ToUInt32(x))
                                               .ToList();

                if (_context.AssignmentGroups.Any(assignmentGroup => assignmentGroup.Name == name))
                {
                    // Assignment group exists already by name
                    ModelState.AddModelError("AssignmentGroup", $"Assignment group with name '{name}' already exists.");
                    return View();
                }

                if (assignmentIds.Count == 0)
                {
                    // At least one assignment is required to create the assignment group
                    ModelState.AddModelError("AssignmentGroup", $"At least one assignment is required to create the assignment group.");
                    return View();
                }

                var assignmentGroup = new AssignmentGroup
                {
                    Name = name,
                    AssignmentIds = assignmentIds,
                };

                // Add assignment group to database
                await _context.AssignmentGroups.AddAsync(assignmentGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("AssignmentGroup", $"Unknown error occurred while creating new assignment group.");
                return View();
            }
        }

        // GET: AssignmentGroupController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
            if (assignmentGroup == null)
            {
                // Failed to retrieve assignment group from database, does it exist?
                ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                return View();
            }

            var assignmentsList = new List<SelectListItem>();
            var assignments = _context.Assignments.ToList();
            foreach (var assignment in assignments)
            {
                var displayText = (string.IsNullOrEmpty(assignment.DeviceGroupName)
                    ? assignment.DeviceUuid
                    : assignment.DeviceGroupName
                ) + $" -> {assignment.InstanceName} ({Utils.FormatAssignmentTime(assignment.Time)}";
                assignmentsList.Add(new SelectListItem
                {
                    Text = displayText,
                    Value = assignment.Id.ToString(),
                    Selected = assignmentGroup.AssignmentIds.Contains(assignment.Id),
                });
            }
            ViewBag.Assignments = assignmentsList;
            return View(assignmentGroup);
        }

        // POST: AssignmentGroupController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
                if (assignmentGroup == null)
                {
                    // Failed to retrieve assignment group from database, does it exist?
                    ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                    return View();
                }

                var name = Convert.ToString(collection["Name"]);
                var assignments = collection["AssignmentIds"].ToList();
                var assignmentIds = assignments.Select(x => Convert.ToUInt32(x))
                                               .ToList();

                if (_context.AssignmentGroups.Any(assignmentGroup => assignmentGroup.Name == name && assignmentGroup.Name != id))
                {
                    // Assignment group exists already by name
                    ModelState.AddModelError("AssignmentGroup", $"Assignment group with name '{name}' already exists.");
                    return View();
                }

                if (assignments.Count == 0)
                {
                    // At least one assignment is required to create the assignment group
                    ModelState.AddModelError("AssignmentGroup", $"At least one assignment is required to create the assignment group.");
                    return View();
                }

                assignmentGroup.Name = name;
                assignmentGroup.AssignmentIds = assignmentIds;

                // Update assignment group to database
                _context.AssignmentGroups.Update(assignmentGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("AssignmentGroup", $"Unknown error occurred while editing assignment group '{id}'.");
                return View();
            }
        }

        // GET: AssignmentGroupController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
            if (assignmentGroup == null)
            {
                // Failed to retrieve assignment group from database, does it exist?
                ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                return View();
            }
            return View(assignmentGroup);
        }

        // POST: AssignmentGroupController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
                if (assignmentGroup == null)
                {
                    // Failed to retrieve assignment group from database, does it exist?
                    ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                    return View();
                }

                // Delete assignment group from database
                _context.AssignmentGroups.Remove(assignmentGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("AssignmentGroup", $"Unknown error occurred while deleting assignment group '{id}'.");
                return View();
            }
        }

        // GET: AssignmentGroupController/Start/5
        public async Task<ActionResult> Start(string id)
        {
            try
            {
                var assignmentGroup = await _context.AssignmentGroups.FindAsync(id);
                if (assignmentGroup == null)
                {
                    // Failed to retrieve geofence from database, does it exist?
                    ModelState.AddModelError("AssignmentGroup", $"Assignment group does not exist with id '{id}'.");
                    return View(assignmentGroup);
                }

                // Start all device assignments in assignment group
                await _assignmentService.StartAssignmentGroupAsync(assignmentGroup);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("AssignmentGroup", $"Unknown error occurred while starting assignment group '{id}'.");
                return View();
            }
        }
    }
}
namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class AssignmentController : Controller
    {
        private readonly ILogger<AssignmentController> _logger;
        private readonly DeviceControllerContext _context;

        public AssignmentController(
            ILogger<AssignmentController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
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
            return View();
        }

        // POST: AssignmentController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
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

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Assignment", $"Unknown error occurred while deleting assignment '{id}'.");
                return View();
            }
        }
    }
}
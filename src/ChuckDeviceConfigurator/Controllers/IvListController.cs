namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class IvListController : Controller
    {
        private readonly ILogger<IvListController> _logger;
        private readonly DeviceControllerContext _context;

        public IvListController(
            ILogger<IvListController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: IvListController
        public ActionResult Index()
        {
            var ivLists = _context.IvLists.ToList();
            return View(new ViewModelsModel<IvList>
            {
                Items = ivLists,
            });
        }

        // GET: IvListController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var ivList = await _context.IvLists.FindAsync(id);
            if (ivList == null)
            {
                // Failed to retrieve IV list from database, does it exist?
                ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                return View();
            }
            return View(ivList);
        }

        // GET: IvListController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: IvListController/Create
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
                ModelState.AddModelError("Geofence", $"Unknown error occurred while creating new IV list.");
                return View();
            }
        }

        // GET: IvListController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var ivList = await _context.IvLists.FindAsync(id);
            if (ivList == null)
            {
                // Failed to retrieve IV list from database, does it exist?
                ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                return View();
            }
            return View(ivList);
        }

        // POST: IvListController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var ivList = await _context.IvLists.FindAsync(id);
                if (ivList == null)
                {
                    // Failed to retrieve IV list from database, does it exist?
                    ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                    return View();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("IvList", $"Unknown error occurred while editing IV list '{id}'.");
                return View();
            }
        }

        // GET: IvListController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var ivList = await _context.IvLists.FindAsync(id);
            if (ivList == null)
            {
                // Failed to retrieve IV list from database, does it exist?
                ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                return View();
            }
            return View(ivList);
        }

        // POST: IvListController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var ivList = await _context.IvLists.FindAsync(id);
                if (ivList == null)
                {
                    // Failed to retrieve IV list from database, does it exist?
                    ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                    return View();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("IvList", $"Unknown error occurred while deleting IV list '{id}'.");
                return View();
            }
        }
    }
}
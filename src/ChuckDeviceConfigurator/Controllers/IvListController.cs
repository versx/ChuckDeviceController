namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.IvListsRole)]
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
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var pokemonIds = Convert.ToString(collection["PokemonIds"]);
                var list = pokemonIds.Replace("<br>", "\r\n")
                                     .Replace("\r\n", "\n")
                                     .Split('\n')
                                     .Select(uint.Parse)
                                     .ToList();

                if (_context.IvLists.Any(iv => iv.Name == name))
                {
                    // IV list exists already by name
                    ModelState.AddModelError("IvList", $"IV list with name '{name}' already exists.");
                    return View();
                }

                var ivList = new IvList
                {
                    Name = name,
                    PokemonIds = list,
                };

                await _context.IvLists.AddAsync(ivList);
                await _context.SaveChangesAsync();

                // TODO: IV list controller

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
            // TODO: Fix issue displaying Pokemon Ids
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

                // Check if new name already exists
                var name = Convert.ToString(collection["Name"]);
                if (ivList.Name != name)
                {
                    if (_context.IvLists.Any(list => list.Name == name && list.Name != id))
                    {
                        ModelState.AddModelError("IvList", $"IV list by name '{name}' already exists, please choose another.");
                        return View();
                    }

                    ivList.Name = name;
                }

                var pokemonIds = Convert.ToString(collection["PokemonIds"])
                                        .Replace("<br>", "\n")
                                        .Replace("<br />", "\n")
                                        .Replace("\r\n", "\n")
                                        .Split('\n')
                                        .Select(s => Convert.ToUInt32(s))
                                        .ToList();

                if (ivList.PokemonIds.Count != pokemonIds.Count) // TODO: Compare whole lists
                {
                    ivList.PokemonIds = pokemonIds;
                }

                _context.Update(ivList);
                await _context.SaveChangesAsync();

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

                // Delete IV list from database
                _context.IvLists.Remove(ivList);
                await _context.SaveChangesAsync();

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
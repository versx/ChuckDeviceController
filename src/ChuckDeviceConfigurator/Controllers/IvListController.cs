namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.IvLists;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    [Authorize(Roles = RoleConsts.IvListsRole)]
    public class IvListController : Controller
    {
        private readonly ILogger<IvListController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IIvListControllerService _ivListService;

        public IvListController(
            ILogger<IvListController> logger,
            DeviceControllerContext context,
            IIvListControllerService ivListService)
        {
            _logger = logger;
            _context = context;
            _ivListService = ivListService;
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
                var list = pokemonIds == "*"
                    ? GeneratePokemonList()
                    : pokemonIds.Replace("<br>", "\r\n")
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

                _ivListService.Add(ivList);

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

                // Check if new name already exists
                var name = Convert.ToString(collection["Name"]);
                if (ivList.Name != name)
                {
                    if (_context.IvLists.Any(list => list.Name == name && list.Name != id))
                    {
                        ModelState.AddModelError("IvList", $"IV list by name '{name}' already exists, please choose another name.");
                        return View();
                    }

                    ivList.Name = name;
                }

                var pokemonIds = Convert.ToString(collection["PokemonIds"]);
                var list = pokemonIds == "*"
                    ? GeneratePokemonList()
                    : pokemonIds.Replace("<br>", "\n")
                                .Replace("<br />", "\n")
                                .Replace("\r\n", "\n")
                                .Split('\n')
                                .Select(uint.Parse)
                                .ToList();

                // Compare list counts or if any elements are different
                if (ivList.PokemonIds.Count != list.Count ||
                    ivList.PokemonIds.IsEqual(list, ignoreOrder: true))
                {
                    ivList.PokemonIds = list;
                }

                _context.Update(ivList);
                await _context.SaveChangesAsync();

                _ivListService.Edit(ivList, id);

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

                _ivListService.Delete(id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("IvList", $"Unknown error occurred while deleting IV list '{id}'.");
                return View();
            }
        }

        private static List<uint> GeneratePokemonList(int start = 1, int end = 999)
        {
            var pokemonIds = Enumerable.Range(start, end - start)
                                       .Select(Convert.ToUInt32)
                                       .ToList();
            return pokemonIds;
        }
    }
}
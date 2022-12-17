namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Localization;
    using ChuckDeviceConfigurator.Services.IvLists;
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;

    [Authorize(Roles = RoleConsts.IvListsRole)]
    public class IvListController : Controller
    {
        #region Variables

        private static readonly IReadOnlyList<string> _ignoreForms = new List<string>
        {
            "Normal",
            "Shadow",
            "Purified",
        };
        private readonly ILogger<IvListController> _logger;
        private readonly IUnitOfWork _uow;
        private readonly IIvListControllerService _ivListService;

        #endregion

        #region Constructor

        public IvListController(
            ILogger<IvListController> logger,
            IUnitOfWork uow,
            IIvListControllerService ivListService)
        {
            _logger = logger;
            _uow = uow;
            _ivListService = ivListService;
        }

        #endregion

        // GET: IvListController
        public async Task<ActionResult> Index()
        {
            var ivLists = await _uow.IvLists.FindAllAsync();
            var model = new List<IvListViewModel>();
            foreach (var ivList in ivLists)
            {
                var images = new List<string>();
                ivList.PokemonIds.Sort();
                foreach (var pokemonId in ivList.PokemonIds)
                {
                    var split = pokemonId.Split(new[] { "_f" }, StringSplitOptions.RemoveEmptyEntries);
                    var id = Convert.ToUInt32(split[0]);
                    var formId = split.Length > 1 ? Convert.ToUInt32(split[1]) : 0;
                    var image = Utils.GetPokemonIcon(id, formId, html: true);
                    images.Add(image);
                }
                model.Add(new IvListViewModel
                {
                    Name = ivList.Name,
                    Pokemon = images,
                });
            };
            return View(new ViewModelsModel<IvListViewModel>
            {
                Items = model,
            });
        }

        // GET: IvListController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var ivList = await _uow.IvLists.FindByIdAsync(id);
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
            ViewData["Pokemon"] = GeneratePokemonList();
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
                var pokemonIds = Convert.ToString(collection["PokemonIds"])
                                        .Split(',')
                                        .ToList();

                if (_uow.IvLists.Any(iv => iv.Name == name))
                {
                    // IV list exists already by name
                    ModelState.AddModelError("IvList", $"IV list with name '{name}' already exists.");
                    return View();
                }

                var ivList = new IvList
                {
                    Name = name,
                    PokemonIds = pokemonIds,
                };

                await _uow.IvLists.AddAsync(ivList);
                await _uow.CommitAsync();

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
            var ivList = await _uow.IvLists.FindByIdAsync(id);
            if (ivList == null)
            {
                // Failed to retrieve IV list from database, does it exist?
                ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                return View();
            }
            ViewData["Pokemon"] = GeneratePokemonList(selectedPokemon: ivList.PokemonIds);
            return View(ivList);
        }

        // POST: IvListController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var ivList = await _uow.IvLists.FindByIdAsync(id);
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
                    if (_uow.IvLists.Any(list => list.Name == name && list.Name != id))
                    {
                        ModelState.AddModelError("IvList", $"IV list by name '{name}' already exists, please choose another name.");
                        return View();
                    }

                    ivList.Name = name;
                }

                var pokemonIds = Convert.ToString(collection["PokemonIds"])
                                        .Split(',')
                                        .ToList();

                // Compare list counts or if any elements are different
                if (ivList.PokemonIds.Count != pokemonIds.Count ||
                    ivList.PokemonIds.IsEqual(pokemonIds, ignoreOrder: true))
                {
                    ivList.PokemonIds = pokemonIds;
                }

                await _uow.IvLists.UpdateAsync(ivList);
                await _uow.CommitAsync();

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
            var ivList = await _uow.IvLists.FindByIdAsync(id);
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
                var ivList = await _uow.IvLists.FindByIdAsync(id);
                if (ivList == null)
                {
                    // Failed to retrieve IV list from database, does it exist?
                    ModelState.AddModelError("IvList", $"IV list does not exist with id '{id}'.");
                    return View();
                }

                // Delete IV list from database
                await _uow.IvLists.RemoveAsync(ivList);
                await _uow.CommitAsync();

                _ivListService.Delete(id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("IvList", $"Unknown error occurred while deleting IV list '{id}'.");
                return View();
            }
        }

        private static List<dynamic> GeneratePokemonList(uint maxPokemonId = Strings.MaxPokemonId, List<string>? selectedPokemon = null)
        {
            var pokemon = new List<dynamic>();

            void AddPokemon(uint pokemonId, uint formId = 0)
            {
                var id = formId > 0
                    ? $"{pokemonId}_f{formId}"
                    : $"{pokemonId}";
                var pkmnName = Translator.Instance.GetPokemonName(pokemonId);
                var formName = formId > 0 ? Translator.Instance.GetFormName(formId) : "";
                var name = string.IsNullOrEmpty(formName)
                    ? pkmnName
                    : $"{pkmnName} - {formName}";
                var image = Utils.GetPokemonIcon(pokemonId, formId);
                var selected = selectedPokemon?.Contains(id) ?? false;
                pokemon.Add(new { id, pokemonId, name, image, selected });
            }

            foreach (var (pokemonId, pkmn) in GameMaster.Instance.Pokedex)
            {
                if (pokemonId > maxPokemonId)
                    continue;

                AddPokemon(pokemonId);
                foreach (var (formId, pkmnForm) in pkmn.Forms)
                {
                    if (_ignoreForms.Contains(pkmnForm.Name))
                        continue;

                    AddPokemon(pokemonId, formId);
                }
            }
            return pokemon;
        }
    }
}
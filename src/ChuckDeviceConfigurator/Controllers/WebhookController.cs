namespace ChuckDeviceConfigurator.Controllers;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using ChuckDeviceConfigurator.Services.Webhooks;
using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;

[Authorize(Roles = RoleConsts.WebhooksRole)]
public class WebhookController : Controller
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IUnitOfWork _uow;
    private readonly IWebhookControllerService _webhookService;

    public WebhookController(
        ILogger<WebhookController> logger,
        IUnitOfWork uow,
        IWebhookControllerService webhookService)
    {
        _logger = logger;
        _uow = uow;
        _webhookService = webhookService;
    }

    // GET: WebhookController
    public async Task<ActionResult> Index()
    {
        var webhooks = await _uow.Webhooks.FindAllAsync();
        return View(new ViewModelsModel<Webhook>
        {
            Items = webhooks.ToList(),
        });
    }

    // GET: WebhookController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        var webhook = await _uow.Webhooks.FindByIdAsync(id);
        if (webhook == null)
        {
            // Failed to retrieve webhook from database, does it exist?
            ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
            return View();
        }
        ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
        return View(webhook);
    }

    // GET: WebhookController/Create
    public async Task<ActionResult> Create()
    {
        var geofences = await _uow.Geofences.FindAsync(geofence => geofence.Type == GeofenceType.Geofence);
        ViewBag.Geofences = geofences;
        ViewBag.WebhookTypes = GetEnumSelectList();
        ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
        return View();
    }

    // POST: WebhookController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Webhook model, IFormCollection collection)
    {
        try
        {
            if (_uow.Webhooks.Any(webhook => webhook.Name == model.Name))
            {
                // Webhook already exists by name
                ModelState.AddModelError("Webhook", $"Webhook with name '{model.Name}' already exists.");
                return View(model);
            }

            var types = collection["Types"]
                .ToString()
                .Split(',')
                .Select(int.Parse)
                .Select(x => (WebhookType)x)
                .ToList();
            var webhookTypes = WebhookType.None;
            types.ForEach(x => webhookTypes |= x);

            var pokemonIds = (model.Data?.PokemonIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var pokestopIds = (model.Data?.PokestopIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var raidPokemonIds = (model.Data?.RaidPokemonIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var gymIds = (model.Data?.GymIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var geofences = model.Geofences.Where(geofence => !string.IsNullOrEmpty(geofence));
            var webhook = new Webhook
            {
                Name = model.Name,
                Url = model.Url,
                Types = webhookTypes,
                Delay = model.Delay,
                Geofences = new(geofences),
                Enabled = model.Enabled,
                Data = new WebhookData
                {
                    PokemonIds = pokemonIds ?? new(),
                    PokestopIds = pokestopIds ?? new(),
                    RaidPokemonIds = raidPokemonIds ?? new(),
                    LureIds = model.Data?.LureIds ?? new(),
                    EggLevels = model.Data?.EggLevels ?? new(),
                    GymTeamIds = model.Data?.GymTeamIds ?? new(),
                    GymIds = gymIds ?? new(),
                    InvasionIds = model.Data?.InvasionIds ?? new(),
                    WeatherConditionIds = model.Data?.WeatherConditionIds ?? new(),
                },
            };

            // Add webhook to database
            await _uow.Webhooks.AddAsync(webhook);
            await _uow.CommitAsync();

            _webhookService.Add(webhook);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Webhook", $"Unknown error occurred while creating new webhook.");
            return View(model);
        }
    }

    // GET: WebhookController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        var webhook = await _uow.Webhooks.FindByIdAsync(id);
        if (webhook == null)
        {
            // Failed to retrieve webhook from database, does it exist?
            ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
            return View();
        }

        var geofences = await _uow.Geofences.FindAsync(geofence => geofence.Type == GeofenceType.Geofence);
        ViewBag.Geofences = geofences;
        ViewBag.WebhookTypes = GetEnumSelectList(webhook.Types);
        ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
        return View(webhook);
    }

    // POST: WebhookController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string id, Webhook model, IFormCollection collection)
    {
        try
        {
            var webhook = await _uow.Webhooks.FindByIdAsync(id);
            if (webhook == null)
            {
                // Failed to retrieve webhook from database, does it exist?
                ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
                return View(model);
            }

            var types = collection["Types"]
                .ToString()
                .Split(',')
                .Select(int.Parse)
                .Select(x => (WebhookType)x)
                .ToList();
            var webhookTypes = WebhookType.None;
            types.ForEach(x => webhookTypes |= x);

            var pokemonIds = (model.Data?.PokemonIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var pokestopIds = (model.Data?.PokestopIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var raidPokemonIds = (model.Data?.RaidPokemonIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var gymIds = (model.Data?.GymIds ?? new())
                .FirstOrDefault()?
                .Replace("\r\n", "\n")
                .Split('\n')
                .ToList();
            var geofences = model.Geofences.Where(geofence => !string.IsNullOrEmpty(geofence));
            webhook.Name = model.Name;
            webhook.Url = model.Url;
            webhook.Types = webhookTypes;
            webhook.Delay = model.Delay;
            webhook.Geofences = new(geofences);
            webhook.Enabled = model.Enabled;
            webhook.Data = new WebhookData
            {
                PokemonIds = pokemonIds ?? new(),
                PokestopIds = pokestopIds ?? new(),
                RaidPokemonIds = raidPokemonIds ?? new(),
                LureIds = model.Data?.LureIds ?? new(),
                EggLevels = model.Data?.EggLevels ?? new(),
                GymTeamIds = model.Data?.GymTeamIds ?? new(),
                GymIds = gymIds ?? new(),
                InvasionIds = model.Data?.InvasionIds ?? new(),
                WeatherConditionIds = model.Data?.WeatherConditionIds ?? new(),
            };

            await _uow.Webhooks.UpdateAsync(webhook);
            await _uow.CommitAsync();

            _webhookService.Edit(webhook, id);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Webhook", $"Unknown error occurred while editing webhook '{id}'.");
            return View(model);
        }
    }

    // GET: WebhookController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        var webhook = await _uow.Webhooks.FindByIdAsync(id);
        if (webhook == null)
        {
            // Failed to retrieve webhook from database, does it exist?
            ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
            return View();
        }
        ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
        return View(webhook);
    }

    // POST: WebhookController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, IFormCollection collection)
    {
        try
        {
            var webhook = await _uow.Webhooks.FindByIdAsync(id);
            if (webhook == null)
            {
                // Failed to retrieve webhook from database, does it exist?
                ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
                return View();
            }

            // Delete webhook from database
            await _uow.Webhooks.RemoveAsync(webhook);
            await _uow.CommitAsync();

            _webhookService.Delete(id);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Webhook", $"Unknown error occurred while deleting webhook '{id}'.");
            return View();
        }
    }

    /// <summary>
    /// Returns a select list for the given <paramref name="type"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{SelectListItem}"/> containing the select list for the given
    /// <paramref name="type"/>.
    /// </returns>
    //public static MultiSelectList GetEnumSelectList(WebhookType? webhookType = null)
    //public static List<SelectListItem> GetEnumSelectList(WebhookType? webhookType = null)
    public static MultiSelectList GetEnumSelectList(WebhookType? webhookType = null)
    {
        var webhookTypes = new List<WebhookType>
        {
            WebhookType.Pokemon,
            WebhookType.Pokestops,
            WebhookType.Lures,
            WebhookType.Invasions,
            WebhookType.Quests,
            WebhookType.AlternativeQuests,
            WebhookType.Gyms,
            WebhookType.GymInfo,
            WebhookType.GymDefenders,
            WebhookType.GymTrainers,
            WebhookType.Eggs,
            WebhookType.Raids,
            WebhookType.Weather,
            WebhookType.Accounts,
        };

        var types = webhookType & (~WebhookType.None);
        var selectList = new List<SelectListItem>();
        //var groupList = new Dictionary<string, SelectListGroup>();
        foreach (var item in webhookTypes)
        {
            var attr = item.GetAttribute<DisplayAttribute>();
            var name = attr?.Name;
            //var groupName = attr?.GroupName;
            var isSelected = (types & item) == item;
            var selectListItem = new SelectListItem
            {
                Text = name,
                Value = Convert.ToString((int)item),
                Selected = isSelected,
            };

            //if (!string.IsNullOrEmpty(groupName))
            //{
            //    if (!groupList.ContainsKey(groupName))
            //    {
            //        groupList[groupName] = new SelectListGroup { Name = groupName };
            //    }

            //    selectListItem.Group = groupList[groupName];
            //}

            selectList.Add(selectListItem);
        }

        var selected = selectList.Where(x => x.Selected).ToList();
        var multiSelectList = new MultiSelectList(selectList, "Value", "Text", selected);
        return multiSelectList;
    }
}
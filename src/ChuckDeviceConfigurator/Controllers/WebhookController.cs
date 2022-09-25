namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Webhooks;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.WebhooksRole)]
    public class WebhookController : Controller
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IWebhookControllerService _webhookService;

        public WebhookController(
            ILogger<WebhookController> logger,
            ControllerDbContext context,
            IWebhookControllerService webhookService)
        {
            _logger = logger;
            _context = context;
            _webhookService = webhookService;
        }

        // GET: WebhookController
        public ActionResult Index()
        {
            var webhooks = _context.Webhooks.ToList();
            return View(new ViewModelsModel<Webhook>
            {
                Items = webhooks,
            });
        }

        // GET: WebhookController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var webhook = await _context.Webhooks.FindAsync(id);
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
        public ActionResult Create()
        {
            var geofences = _context.Geofences.Where(geofence => geofence.Type == GeofenceType.Geofence)
                                              .ToList();
            ViewBag.Geofences = geofences;
            ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
            return View();
        }

        // POST: WebhookController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Webhook model)
        {
            try
            {
                if (_context.Webhooks.Any(webhook => webhook.Name == model.Name))
                {
                    // Webhook already exists by name
                    ModelState.AddModelError("Webhook", $"Webhook with name '{model.Name}' already exists.");
                    return View(model);
                }

                var geofences = model.Geofences.Where(geofence => !string.IsNullOrEmpty(geofence));
                var webhook = new Webhook
                {
                    Name = model.Name,
                    Url = model.Url,
                    Types = model.Types,
                    Delay = model.Delay,
                    Geofences = new(geofences),
                    Enabled = model.Enabled,
                    Data = new WebhookData
                    {
                        PokemonIds = model.Data?.PokemonIds ?? new(),
                        PokestopIds = model.Data?.PokestopIds ?? new(),
                        RaidPokemonIds = model.Data?.RaidPokemonIds ?? new(),
                        LureIds = model.Data?.LureIds ?? new(),
                        EggLevels = model.Data?.EggLevels ?? new(),
                        GymTeamIds = model.Data?.GymTeamIds ?? new(),
                        InvasionIds = model.Data?.InvasionIds ?? new(),
                        WeatherConditionIds = model.Data?.WeatherConditionIds ?? new(),
                    },
                };

                // Add webhook to database
                await _context.AddAsync(webhook);
                await _context.SaveChangesAsync();

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
            var webhook = await _context.Webhooks.FindAsync(id);
            if (webhook == null)
            {
                // Failed to retrieve webhook from database, does it exist?
                ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
                return View();
            }

            var geofences = _context.Geofences.Where(geofence => geofence.Type == GeofenceType.Geofence)
                                              .ToList();
            ViewBag.Geofences = geofences;
            ModelState.Remove(nameof(Webhook.GeofenceMultiPolygons));
            return View(webhook);
        }

        // POST: WebhookController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, Webhook model)
        {
            try
            {
                var webhook = await _context.Webhooks.FindAsync(id);
                if (webhook == null)
                {
                    // Failed to retrieve webhook from database, does it exist?
                    ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
                    return View(webhook);
                }

                var geofences = model.Geofences.Where(geofence => !string.IsNullOrEmpty(geofence));
                webhook.Name = model.Name;
                webhook.Url = model.Url;
                webhook.Types = model.Types;
                webhook.Delay = model.Delay;
                webhook.Geofences = new(geofences);
                webhook.Enabled = model.Enabled;
                webhook.Data = new WebhookData
                {
                    PokemonIds = model.Data?.PokemonIds ?? new(),
                    PokestopIds = model.Data?.PokestopIds ?? new(),
                    RaidPokemonIds = model.Data?.RaidPokemonIds ?? new(),
                    LureIds = model.Data?.LureIds ?? new(),
                    EggLevels = model.Data?.EggLevels ?? new(),
                    GymTeamIds = model.Data?.GymTeamIds ?? new(),
                    InvasionIds = model.Data?.InvasionIds ?? new(),
                    WeatherConditionIds = model.Data?.WeatherConditionIds ?? new(),
                };

                _context.Update(webhook);
                await _context.SaveChangesAsync();

                _webhookService.Edit(webhook, id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Webhook", $"Unknown error occurred while editing webhook '{id}'.");
                return View();
            }
        }

        // GET: WebhookController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var webhook = await _context.Webhooks.FindAsync(id);
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
                var webhook = await _context.Webhooks.FindAsync(id);
                if (webhook == null)
                {
                    // Failed to retrieve webhook from database, does it exist?
                    ModelState.AddModelError("Webhook", $"Webhook does not exist with id '{id}'.");
                    return View();
                }

                // Delete webhook from database
                _context.Webhooks.Remove(webhook);
                await _context.SaveChangesAsync();

                _webhookService.Delete(id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Webhook", $"Unknown error occurred while deleting webhook '{id}'.");
                return View();
            }
        }
    }
}

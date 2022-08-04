namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Webhooks;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.WebhooksRole)]
    public class WebhookController : Controller
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IWebhookControllerService _webhookService;

        public WebhookController(
            ILogger<WebhookController> logger,
            DeviceControllerContext context,
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
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var url = Convert.ToString(collection["Url"]);
                var types = Convert.ToString(collection["Types"]);
                var webhookTypes = types.Split(',')
                                        .Where(type => !string.IsNullOrEmpty(type))
                                        .Select(type => (WebhookType)Convert.ToUInt32(type))
                                        .ToList();
                var delay = Convert.ToDouble(collection["Delay"]);
                var geofences = Convert.ToString(collection["Geofences"]).Split(',');
                var enabled = collection["Enabled"].Contains("true");

                if (_context.Webhooks.Any(webhook => webhook.Name == name))
                {
                    // Webhook already exists by name
                    ModelState.AddModelError("Webhook", $"Webhook with name '{name}' already exists.");
                    return View();
                }
                var webhook = new Webhook
                {
                    Name = name,
                    Url = url,
                    Types = webhookTypes,
                    Delay = delay,
                    Geofences = new(geofences),
                    Enabled = enabled,
                    Data = new WebhookData(),
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
                return View();
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
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
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

                var name = Convert.ToString(collection["Name"]);
                var url = Convert.ToString(collection["Url"]);
                var types = Convert.ToString(collection["Types"]);
                var webhookTypes = types.Split(',')
                                        .Where(type => !string.IsNullOrEmpty(type))
                                        .Select(type => (WebhookType)Convert.ToUInt32(type))
                                        .ToList();
                var delay = Convert.ToDouble(collection["Delay"]);
                var geofences = Convert.ToString(collection["Geofences"]).Split(',');
                var enabled = collection["Enabled"].Contains("true");

                webhook.Name = name;
                webhook.Url = url;
                webhook.Types = webhookTypes;
                webhook.Delay = delay;
                webhook.Geofences = new(geofences);
                webhook.Enabled = enabled;
                webhook.Data = new WebhookData();

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

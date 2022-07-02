namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.WebhooksRole)]
    public class WebhookController : Controller
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly DeviceControllerContext _context;

        public WebhookController(
            ILogger<WebhookController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
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
            return View(webhook);
        }

        // GET: WebhookController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: WebhookController/Create
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
                    return View();
                }
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

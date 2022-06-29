namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class WebhookController : Controller
    {
        private readonly DeviceControllerContext _context;

        public WebhookController(DeviceControllerContext context)
        {
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
                return View();
            }
        }

        // GET: WebhookController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var webhook = await _context.Webhooks.FindAsync(id);
            return View(webhook);
        }

        // POST: WebhookController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: WebhookController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var webhook = await _context.Webhooks.FindAsync(id);
            return View(webhook);
        }

        // POST: WebhookController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}

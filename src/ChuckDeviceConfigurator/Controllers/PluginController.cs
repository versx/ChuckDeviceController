namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    public class PluginController : Controller
    {
        // GET: PluginController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PluginController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PluginController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PluginController/Create
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

        // GET: PluginController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PluginController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: PluginController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PluginController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
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
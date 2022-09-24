namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Common;

    [Controller]
    [Authorize(Roles = RoleConsts.ApiKeysRole)]
    public class ApiKeyController : Controller
    {
        // GET: ApiKeyController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ApiKeyController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ApiKeyController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ApiKeyController/Create
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

        // GET: ApiKeyController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ApiKeyController/Edit/5
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

        // GET: ApiKeyController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ApiKeyController/Delete/5
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
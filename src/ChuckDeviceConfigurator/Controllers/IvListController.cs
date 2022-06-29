namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class IvListController : Controller
    {
        private readonly DeviceControllerContext _context;

        public IvListController(DeviceControllerContext context)
        {
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
        public ActionResult Details(string id)
        {
            return View();
        }

        // GET: IvListController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: IvListController/Create
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

        // GET: IvListController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var ivList = await _context.IvLists.FindAsync(id);
            return View(ivList);
        }

        // POST: IvListController/Edit/5
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

        // GET: IvListController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var ivList = await _context.IvLists.FindAsync(id);
            return View(ivList);
        }

        // POST: IvListController/Delete/5
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
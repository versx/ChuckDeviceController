﻿namespace ChuckDeviceConfigurator.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;

    [Authorize(Roles = RoleConsts.DefaultRole)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DeviceControllerContext _deviceContext;
        private readonly MapDataContext _mapContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            DeviceControllerContext deviceContext,
            MapDataContext mapContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _deviceContext = deviceContext;
            _mapContext = mapContext;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Accounts = (ulong)_deviceContext.Accounts.LongCount(),
                Assignments = (ulong)_deviceContext.Assignments.LongCount(),
                AssignmentGroups = (ulong)_deviceContext.AssignmentGroups.LongCount(),
                Devices = (ulong)_deviceContext.Devices.LongCount(),
                DeviceGroups = (ulong)_deviceContext.DeviceGroups.LongCount(),
                Geofences = (ulong)_deviceContext.Geofences.LongCount(),
                Instances = (ulong)_deviceContext.Instances.LongCount(),
                IvLists = (ulong)_deviceContext.IvLists.LongCount(),
                Webhooks = (ulong)_deviceContext.Webhooks.LongCount(),
                Users = (ulong)_userManager.Users.LongCount(),

                Gyms = (ulong)_mapContext.Gyms.LongCount(),
                GymDefenders = (ulong)_mapContext.GymDefenders.LongCount(),
                GymTrainers = (ulong)_mapContext.GymTrainers.LongCount(),
                // TODO: Raids
                Incidents = (ulong)_mapContext.Incidents.LongCount(),
                Pokemon = (ulong)_mapContext.Pokemon.LongCount(),
                Pokestops = (ulong)_mapContext.Pokestops.LongCount(),
                // TODO: Lures
                // TODO: Quests
                Cells = (ulong)_mapContext.Cells.LongCount(),
                Spawnpoints = (ulong)_mapContext.Spawnpoints.LongCount(),
                Weather = (ulong)_mapContext.Weather.LongCount(),
            };
            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(model);
        }
    }
}
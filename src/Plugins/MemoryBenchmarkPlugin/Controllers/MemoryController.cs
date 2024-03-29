﻿namespace MemoryBenchmarkPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceController.Common;

// Reference: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/compare-metric-apis?view=aspnetcore-6.0

[Authorize(Roles = RoleConsts.BenchmarksRole)]
public class MemoryController : Controller
{
    public MemoryController() 
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

[Controller]
[Authorize(Roles = RoleConsts.BenchmarksRole)]
public class DataBenchmarkController : Controller
{
    // TODO: Consider using gRPC to retrieve the proto/data statistics (add controller endpoint to config)
    private const string StatsEndpoint = "http://localhost:8882/api/Statistics";
    private const ushort DefaultTimeoutS = 15;

    // GET: DataBenchmarkController
    public async Task<ActionResult> Index()
    {
        var data = await NetUtils.GetAsync(StatsEndpoint, timeoutS: DefaultTimeoutS);
        var model = data?.FromJson<ProtoDataStatisticsViewModel>();
        return View(model);
    }

    // GET: DataBenchmarkController/Clear
    public async Task<ActionResult> Reset()
    {
        _ = await NetUtils.GetAsync(StatsEndpoint + "/Reset");
        return RedirectToAction(nameof(Index));
    }
}
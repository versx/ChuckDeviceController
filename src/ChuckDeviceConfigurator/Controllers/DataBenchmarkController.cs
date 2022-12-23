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
    private const string StatisticsEndpoint = "http://localhost:8882/api/Statistics";
    private const ushort DefaultTimeoutS = 15;

    // GET: DataBenchmarkController
    public async Task<ActionResult> Index()
    {
        var data = await NetUtils.GetAsync(StatisticsEndpoint, timeoutS: DefaultTimeoutS);
        var model = data?.FromJson<ProtoDataStatisticsViewModel>();
        return View(model);
    }

    // GET: DataBenchmarkController/Reset
    public async Task<ActionResult> Reset()
    {
        _ = await NetUtils.GetAsync(StatisticsEndpoint + "/Reset");
        return RedirectToAction(nameof(Index));
    }
}
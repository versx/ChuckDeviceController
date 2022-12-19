namespace RequestBenchmarkPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceController.Common;

using Data.Contexts;
using Services;

[Authorize(Roles = RoleConsts.BenchmarksRole)]
public class RequestTimeController : Controller
{
    //private readonly ILogger<RequestTimeController> _logger;
    private readonly RequestTimesDbContext _context;
    private readonly IRequestBenchmarkService _benchmarkService;

    public RequestTimeController(
        //ILogger<RequestTimeController> logger,
        RequestTimesDbContext context,
        IRequestBenchmarkService benchmarkService)
    {
        //_logger = logger;
        _context = context;
        _benchmarkService = benchmarkService;
    }

    public IActionResult Index()
    {
        var timings = _context.RequestTimes.ToList();
        return View(timings);
    }

    public async Task<IActionResult> Details(string route)
    {
        var timing = await _context.RequestTimes.FindAsync(route);
        if (timing == null)
        {
            return View();
        }
        return View(timing);
    }

    public async Task<IActionResult> Delete(string route)
    {
        var time = await _context.RequestTimes.FindAsync(route);
        if (time != null)
        {
            _context.RequestTimes.Remove(time);
            await _context.SaveChangesAsync();

            _benchmarkService.Delete(route);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Clear()
    {
        _context.RemoveRange(_context.RequestTimes);
        await _context.SaveChangesAsync();

        _benchmarkService.ClearBenchmarks();

        return RedirectToAction(nameof(Index));
    }
}
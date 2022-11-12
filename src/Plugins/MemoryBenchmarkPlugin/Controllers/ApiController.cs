namespace MemoryBenchmarkPlugin.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Mvc;

    // Credits: https://github.com/sebastienros/memoryleak
    // Reference: https://docs.microsoft.com/en-us/aspnet/core/performance/memory?view=aspnetcore-6.0

    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private const double RefreshRate = 1 * 1000;

        #region Variables

        private static readonly Process _process = Process.GetCurrentProcess();
        private static TimeSpan _oldCpuTime = TimeSpan.Zero;
        private static DateTime _lastMonitorTime = DateTime.UtcNow;
        private static DateTime _lastRpsTime = DateTime.UtcNow;
        private static double _cpu = 0, _rps = 0;
        private static long _requests = 0;

        #endregion

        #region Constructor

        public ApiController()
        {
            Interlocked.Increment(ref _requests);
        }

        #endregion

        #region Public Methods

        [HttpGet("collect")]
        public IActionResult GetCollect()
        {
            // TODO: Add generation and collection mode support
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return Ok();
        }

        [HttpGet("diagnostics")]
        public IActionResult GetDiagnostics()
        {
            var now = DateTime.UtcNow;
            _process.Refresh();

            var cpuElapsedTime = now.Subtract(_lastMonitorTime).TotalMilliseconds;

            if (cpuElapsedTime > RefreshRate)
            {
                var newCpuTime = _process.TotalProcessorTime;
                var elapsedCpu = (newCpuTime - _oldCpuTime).TotalMilliseconds;
                _cpu = elapsedCpu * 100 / Environment.ProcessorCount / cpuElapsedTime;

                _lastMonitorTime = now;
                _oldCpuTime = newCpuTime;
            }

            var rpsElapsedTime = now.Subtract(_lastRpsTime).TotalMilliseconds;
            if (rpsElapsedTime > RefreshRate)
            {
                _rps = _requests * 1000 / rpsElapsedTime;
                Interlocked.Exchange(ref _requests, 0);
                _lastRpsTime = now;
            }

            var diagnostics = new
            {
                PID = _process.Id,

                // The memory occupied by objects.
                Allocated = GC.GetTotalMemory(false),

                // The working set includes both shared and private data. The shared data includes the pages that contain all the 
                // instructions that the process executes, including instructions in the process modules and the system libraries.
                WorkingSet = _process.WorkingSet64,

                // The value returned by this property represents the current size of memory used by the process, in bytes, that 
                // cannot be shared with other processes.
                PrivateBytes = _process.PrivateMemorySize64,

                // The number of generation 0 collections
                Gen0 = GC.CollectionCount(0),

                // The number of generation 1 collections
                Gen1 = GC.CollectionCount(1),

                // The number of generation 2 collections
                Gen2 = GC.CollectionCount(2),

                CPU = _cpu,

                RPS = _rps
            };

            return new ObjectResult(diagnostics);
        }

        #endregion
    }
}
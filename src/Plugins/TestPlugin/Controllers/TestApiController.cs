namespace TestPlugin.Controllers
{
    using System.Net.Mime;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Common;

    // TODO: Add to separate TestApiPlugin

    [ApiController] // Web API only, no Mvc Views used
    [Produces(MediaTypeNames.Application.Json)] // Only produces json responses (can put per method vs class if desired)
    public class TestApiController : ControllerBase
    {
        // Just a note: classes that inherit an interface (aka contract) will only return
        // the properties that the interface knows about.
        // We set the coordinates cache list to static because controllers are scope based.
        // When we call the POST route and check if the 4 additional coordinates were added
        // it will still only show the default initialized values. Setting the variable to
        // static will make it persist between requests.
        private static List<ICoordinate> _coords = new()
        {
            new Coordinate(1.1, 1.1),
            new Coordinate(2.2, 2.2),
            new Coordinate(3.3, 3.3),
            new Coordinate(4.4, 4.4),
        };
        private readonly ILogger<TestApiController> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public TestApiController(ILogger<TestApiController> logger)
        {
            _logger = logger;
        }

        // If a route is specified explicitly i.e. '[HttpGet("/api/coords")]' the
        // method name is not very important for the request. That goes for 'Controller'
        // and 'ControllerBase' inherited controllers. When no explicit route is
        // set i.e. '[HttpGet]' the method becomes the route slug. For this example if
        // it wasn't explicitly set the route would be '/TestApi/PrintCoordinates'. The
        // actual route is '/api/coords' for this example.
        [HttpGet("api/coords")]
        public IActionResult PrintCoordinates()
        {
            foreach (var coord in _coords)
            {
                _logger.LogInformation($"Coord: {coord}");
            }

            // Should return: '[{ "lat": 1.1, "lon": 1.1 }, { "lat": 2.2, "lon": 2.2 }, etc]'
            return new JsonResult(_coords);
        }

        [HttpPost("api/coords")]
        public JsonResult AddCoordinates([FromBody] List<ICoordinate> coords)
        {
            foreach (var coord in coords)
            {
                _logger.LogInformation($"Received coord: {coord}");
            }
            // Add coordinates from response body to existing cached list
            _coords.AddRange(_coords);

            // Should return: '{ "status": "OK" }'
            return new JsonResult(new
            {
                status = "OK",
            });
        }
    }
}
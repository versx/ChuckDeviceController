namespace TestPlugin.Controllers
{
    using System.Net.Mime;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Geometry.Models.Contracts;

    // TODO: Add to separate TestApiPlugin

    // Web API only, no Mvc Views used
    [ApiController]
    // Only produces json responses (can put per method vs class if desired)
    [Produces(MediaTypeNames.Application.Json)]
    public class TestApiController : ControllerBase
    {
        // Just a note: classes that inherit an interface (aka contract) will only populate
        // the properties that the interface contract knows about.
        // We set the coordinates cache list to static because controllers are typically
        // scope based, setting to static will retain the same value between multiple
        // instances of this API controller.
        // When we call the POST route and check if the 4 additional coordinates were added,
        // it will still only show the default initialized values. Setting the variable to
        // static will make it persist between requests/instances.
        private static readonly List<ICoordinate> _coords = new()
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
        // set i.e. '[HttpGet]' the method name becomes the route slug. For this example
        // if it wasn't explicitly set the route would be '/TestApi/PrintCoordinates'.
        // The actual route is '/api/coords' in this API controller example.
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

        // The `[FromBody]` parameter attribute signifies the `coords` value should be pulled
        // from the body of the request. Other valid parameter attributes include:
        // - [FromBody
        // - [FromForm]
        // - [FromHeader]
        // - [FromQuery]
        // - [FromRoute]
        // - [FromServices]
        // - [AsParameters]
        // If no attribute is specified for the route methods parameter explicitly, the value
        // by default will be pulled from the Url query. i.e. `user/123`.
        // If no attribute is specified for the route methods parameter expllicitly but a
        // parameter pattern is set in the route method attribute (i.e. `[HttpPost("user/{id}")]`),
        // the value will be pulled from the parameters.
        // 
        // More information:
        // - https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-7.0#binding-source-parameter-inference-1
        // - https://learn.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2
        // - https://learn.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/routing-in-aspnet-web-api
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

        [HttpGet("api/stats")]
        public JsonResult Stats()
        {
            return new JsonResult(new
            {
                count = _coords.Count,
            });
        }
    }
}
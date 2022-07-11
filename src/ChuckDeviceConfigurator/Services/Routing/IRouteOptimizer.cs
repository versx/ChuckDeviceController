namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public interface IRouteOptimizer
    {
        bool IncludeGyms { get; }

        bool IncludePokestops { get; }

        bool IncludeSpawnpoints { get; }

        bool IncludeS2Cells { get; }

        bool IncludeNests { get; }

        bool OptimizePolygons { get; }

        bool OptimizeCircles { get; }

        IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        Task<List<Coordinate>> GenerateRouteAsync(RouteOptimizerOptions options);
    }
}

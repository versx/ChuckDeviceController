namespace ChuckDeviceConfigurator.Services.Routing.Walking
{
    using ChuckDeviceController.Geometry.Models;

    public delegate void UpdatePositionDelegate(double lat, double lng, double speed);

    public interface IWalkStrategy
    {
        public string RouteName { get; }

        public List<Coordinate> Points { get; set; }

        public event UpdatePositionDelegate UpdatePositionEvent;

        public Task Walk(Coordinate destinationLocation, Func<Task> functionExecutedWhileWalking, CancellationToken cancellationToken, double customWalkingSpeed = 0.0);

        public double CalculateDistance(Coordinate source, Coordinate destination);
    }
}
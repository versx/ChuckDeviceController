/*
namespace ChuckDeviceController.Services.Routes.Walk
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Entities;

    public interface IWalkStrategy
    {
        public string RouteName { get; }

        public List<Coordinate> Points { get; set; }

        public event UpdatePositionDelegate UpdatePositionEvent;

        public Task Walk(Coordinate destinationLocation, Func<Task> functionExecutedWhileWalking, CancellationToken cancellationToken, double customWalkingSpeed = 0.0);

        public double CalculateDistance(Coordinate source, Coordinate destination);
    }
}
*/
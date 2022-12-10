﻿namespace ChuckDeviceController.JobControllers.Models
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Geometry.Models.Contracts;

    internal class SmartRaidGym
    {
        public IGym Gym { get; set; }

        public ulong Updated { get; set; }

        public ICoordinate Coordinate { get; set; }

        public SmartRaidGym(IGym gym, ulong updated, ICoordinate coordinate)
        {
            Gym = gym;
            Updated = updated;
            Coordinate = coordinate;
        }
    }
}
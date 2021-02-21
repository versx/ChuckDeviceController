namespace ChuckDeviceController.Geofence.Models
{
    using System;
    using System.Collections.Generic;

    public class Polygon : List<double>
    {
        public Polygon(double lat, double lon)
        {
            Add(lat);
            Add(lon);
        }
    }
}
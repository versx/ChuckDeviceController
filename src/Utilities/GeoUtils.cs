namespace ChuckDeviceController.Utilities
{
    using NetTopologySuite.Features;
    using NetTopologySuite.Geometries;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Coordinate = ChuckDeviceController.Data.Entities.Coordinate;

    public static class GeoUtils
    {
        /// <summary>
        /// Creates a Feature containing a Polygon created using the provided Locations
        /// </summary>
        public static Feature LocationsToFeature(IEnumerable<Coordinate> locations, IAttributesTable attributes = default)
        {
            List<NetTopologySuite.Geometries.Coordinate> coordinateList = locations.Select(c => new NetTopologySuite.Geometries.Coordinate(c.Latitude, c.Longitude)).ToList();
            if (coordinateList.Count < 3)
            {
                throw new ArgumentException("At least three locations are required", nameof(locations));
            }

            if (!coordinateList[0].Equals2D(coordinateList.Last(), double.Epsilon))
            {
                // A closed linear ring requires the same point at the start and end of the list
                coordinateList.Add(coordinateList[0]);
            }

            LinearRing polygonRing = GeometryFactory.Default.CreateLinearRing(coordinateList.ToArray());
            Polygon polygon = new Polygon(polygonRing);
            Feature feature = new Feature(polygon, attributes ?? new AttributesTable());
            return feature;
        }
    }
}
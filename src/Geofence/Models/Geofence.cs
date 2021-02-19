namespace ChuckDeviceController.Geofence.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NetTopologySuite.Features;
    using NetTopologySuite.Geometries;

    using Coordinate = ChuckDeviceController.Data.Entities.Coordinate;
    using ChuckDeviceController.Utilities;

    public class Geofence
    {
        private const string DefaultName = "Unnamed";

        #region Properties

        /// <summary>
        /// Gets or sets the name of the geofence
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The filename from which this geofence originated
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets the FeatureCollection containing the geometry which represents this geofence
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the geometry representing the smallest possible bounding box which contains all points of this geofence
        /// </summary>
        public Geometry BBox { get; }

        /// <summary>
        /// Gets or sets the priority of this geofence. Higher-priority geofences will take precedence
        /// when determining which geofence a particular location falls within if it falls within multiple.
        /// </summary>
        public int Priority { get; set; }

        public IReadOnlyList<Coordinate> Coordinates { get; set; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class by name
        /// </summary>
        /// <param name="name">Name of geofence</param>
        public Geofence(string name = default)
        {
            Name = name ?? DefaultName;
            Priority = 0;
            Feature = new Feature();
            BBox = Geometry.DefaultFactory.CreateEmpty(Dimension.False);
        }

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class from a GeoJSON feature.
        /// If the feature has a "name" attribute, this geofence's name will be set from that.
        /// </summary>
        public Geofence(IFeature feature)
        {
            Name = feature.Attributes["name"]?.ToString() ?? DefaultName;
            Feature = feature;
            BBox = feature.Geometry.Envelope;

            try
            {
                Priority = Convert.ToInt32(feature.Attributes["priority"]);
            }
            catch
            {
                Priority = 0;
            }
        }

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class with name and polygons
        /// </summary>
        /// <param name="name">Name of geofence</param>
        /// <param name="coordinates">Location polygons of geofence</param>
        public Geofence(string name, List<Coordinate> coordinates) : this(name)
        {
            Coordinates = coordinates;
            Feature = GeoUtils.LocationsToFeature(coordinates);
            BBox = Feature.Geometry.Envelope;
        }

        #endregion

        public static Geofence FromPolygon(List<Coordinate> polygon)
        {
            return new Geofence(null, polygon);
        }

        public static List<Geofence> FromPolygons(List<List<Coordinate>> polygons)
        {
            return polygons.Select(p => FromPolygon(p))
                           .ToList();
        }

        public static Geofence FromMultiPolygon(MultiPolygon multiPolygon)
        {
            var polygon = multiPolygon.Select(x => new Coordinate(x[0], x[1]))
                                      .ToList();
            return FromPolygon(polygon);            
        }

        public static List<Geofence> FromMultiPolygons(List<MultiPolygon> multiPolygons)
        {
            return multiPolygons.Select(p => FromMultiPolygon(p))
                                .ToList();
        }
    }
}
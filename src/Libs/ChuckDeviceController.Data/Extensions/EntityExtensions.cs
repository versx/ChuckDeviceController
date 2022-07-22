namespace ChuckDeviceController.Data.Extensions
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public static class EntityExtensions
    {
        public static Coordinate ToCoordinate(this ICoordinateEntity entity)
        {
            var coord = new Coordinate(entity.Latitude, entity.Longitude);
            return coord;
        }
    }
}
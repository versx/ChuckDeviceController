namespace ChuckDeviceController.Data.Extensions;

using ChuckDeviceController.Common.Abstractions;
using ChuckDeviceController.Geometry.Models;

public static class EntityExtensions
{
    /// <summary>
    ///     Gets a <seealso cref="Coordinate"/> object from the entities
    ///     Latitude and Longitude properties
    /// </summary>
    /// <param name="entity">
    ///     Database entity that inherits from <seealso cref="ICoordinateEntity"/>
    /// </param>
    /// <returns>
    ///     Returns a <seealso cref="Coordinate"/> object from the provided entity
    /// </returns>
    public static Coordinate ToCoordinate(this ICoordinateEntity entity)
    {
        var coord = new Coordinate(entity.Latitude, entity.Longitude);
        return coord;
    }
}
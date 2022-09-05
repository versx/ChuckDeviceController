namespace ChuckDeviceConfigurator.Extensions
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Plugin;

    public static class TypeExtensions
    {
        public static object[]? GetJobControllerConstructorArgs(this Type jobControllerType, IInstance instance, IReadOnlyList<Geofence> geofences)
        {
            var attributes = jobControllerType.GetCustomAttributes(typeof(GeofenceTypeAttribute), false);
            if (!(attributes?.Any() ?? false))
            {
                // No geofence attributes specified but is required
                return null;
            }

            object[]? args = null;
            var attr = attributes!.FirstOrDefault() as GeofenceTypeAttribute;

            switch (attr?.Type)
            {
                case GeofenceType.Circle:
                    var circles = geofences.ConvertToCoordinates();
                    args = new object[] { instance, circles };
                    break;
                case GeofenceType.Geofence:
                    var (_, polyCoords) = geofences.ConvertToMultiPolygons();
                    args = new object[] { instance, polyCoords };
                    break;
            }
            return args;
        }
    }
}
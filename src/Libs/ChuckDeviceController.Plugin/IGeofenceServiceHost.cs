namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Geometry.Models.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public interface IGeofenceServiceHost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Task CreateGeofenceAsync(IGeofence options);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IGeofence> GetGeofenceAsync(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="multiPolygons"></param>
        /// <returns></returns>
        bool IsPointInMultiPolygons(ICoordinate coord, IEnumerable<IMultiPolygon> multiPolygons);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="multiPolygon"></param>
        /// <returns></returns>
        bool IsPointInMultiPolygon(ICoordinate coord, IMultiPolygon multiPolygon);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        bool IsPointInPolygon(ICoordinate coord, IEnumerable<ICoordinate> coordinates);
    }
}
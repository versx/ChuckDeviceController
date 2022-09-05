namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data.Contracts;

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
    }
}
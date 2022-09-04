namespace ChuckDeviceController.Plugin
{
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
        Task CreateGeofenceAsync(IGeofenceCreationOptions options);
    }
}
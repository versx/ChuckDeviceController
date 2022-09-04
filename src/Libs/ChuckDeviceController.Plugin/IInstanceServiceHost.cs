namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public interface IInstanceServiceHost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Task CreateInstanceTypeAsync(IInstanceCreationOptions options);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        Task AddInstanceAsync(IInstance instance);
    }
}
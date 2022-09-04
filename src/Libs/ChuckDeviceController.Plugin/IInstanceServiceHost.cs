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
        Task CreateInstanceTypeAsync(IInstance options);
    }
}
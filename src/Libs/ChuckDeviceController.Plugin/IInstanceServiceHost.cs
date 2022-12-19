namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Abstractions;

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
    Task CreateInstanceAsync(IInstance options);
}
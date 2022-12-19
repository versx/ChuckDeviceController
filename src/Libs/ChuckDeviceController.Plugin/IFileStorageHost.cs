namespace ChuckDeviceController.Plugin;

/// <summary>
///     Interface contract used for reading data from as well as
///     persisting data to storage. The type of storage used will
///     depend on the implementation.
/// </summary>
public interface IFileStorageHost : ISaveData, ILoadData
{
}
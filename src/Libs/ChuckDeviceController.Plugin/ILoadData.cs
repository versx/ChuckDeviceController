namespace ChuckDeviceController.Plugin;

/// <summary>
/// 
/// </summary>
public interface ILoadData
{
    /// <summary>
    ///     Loads file data of type T from the plugin's folder.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of file data to be loaded.
    /// </typeparam>
    /// <param name="folderName">
    ///     Sub folder within plugin's folder, optional. If not set,
    ///     searches root of plugin's folder.
    /// </param>
    /// <param name="fileName">
    ///     File name of storage file to load, including extension
    ///     otherwise generic '.dat' extension will be appended.
    /// </param>
    /// <returns>
    ///     Type of data to be loaded or default type if exception occurs.
    /// </returns>
    T Load<T>(string folderName, string fileName);
}

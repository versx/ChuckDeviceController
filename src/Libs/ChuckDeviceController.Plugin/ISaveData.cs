namespace ChuckDeviceController.Plugin;

/// <summary>
/// Interface contract used by <seealso cref="IFileStorageHost"/>
/// to save data to the file system.
/// </summary>
public interface ISaveData
{
    /// <summary>
    ///     Saves file data of type T to the plugin's folder.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of data to be saved.
    /// </typeparam>
    /// <param name="data">
    ///     File data to be saved.
    /// </param>
    /// <param name="folderName">
    ///     Sub folder within plugin's folder, optional. If not set,<br />
    ///     uses root of plugin's folder.
    /// </param>
    /// <param name="name">
    ///     File name of storage file to save, including extension<br />
    ///     otherwise generic '.dat' extension will be appended.
    /// </param>
    /// <param name="prettyPrint">
    /// </param>
    /// <returns>
    ///     Returns <code>true</code> if successful, otherwise <code>false</code>.
    /// </returns>
    bool Save<T>(T data, string folderName, string name, bool prettyPrint = false);
}
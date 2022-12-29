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
    ///     Sub folder within plugin's folder, optional. If not set, uses root of plugin's folder.
    /// </param>
    /// <param name="name">
    ///     File name of storage file to save, including extension otherwise generic '.dat' extension will be appended.
    /// </param>
    /// <param name="prettyPrint">
    ///     Determines whether or not to 'pretty print' the JSON file to readable format.
    /// </param>
    /// <returns>
    ///     Returns <c>true</c> if successful, otherwise <c>false</c>.
    /// </returns>
    bool Save<T>(T data, string folderName, string name, bool prettyPrint = false);
}
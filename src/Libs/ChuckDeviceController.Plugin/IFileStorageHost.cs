namespace ChuckDeviceController.Plugin
{
    /// <summary>
    ///     Interface contract used for reading data from as well as
    ///     persisting data to storage. The type of storage used will
    ///     depend on the implementation.
    /// </summary>
    public interface IFileStorageHost
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

        /// <summary>
        ///     Saves file data of type T to the plugin's folder.
        /// </summary>
        /// <typeparam name="T">
        ///     Type of data to be saved.
        /// </typeparam>
        /// <param name="data">
        ///     File data to be saved.
        /// </param>
        /// <param name="location">
        ///     Sub folder within plugin's folder, optional. If not set,
        ///     uses root of plugin's folder.
        /// </param>
        /// <param name="name">
        ///     File name of storage file to save, including extension
        ///     otherwise generic '.dat' extension will be appended.
        /// </param>
        /// <returns>
        ///     Returns <code>true</code> if successful, otherwise <code>false</code>.
        /// </returns>
        bool Save<T>(T data, string location, string name);
    }
}
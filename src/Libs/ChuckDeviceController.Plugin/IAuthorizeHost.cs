namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAuthorizeHost
    {
        /// <summary>
        ///     Registers a custom user role with the host application.
        /// </summary>
        /// <param name="name">
        ///     The name of the role to register.
        /// </param>
        /// <param name="displayIndex">
        ///     Display index value when listing roles.
        /// </param>
        /// <returns>
        ///     Returns a value determining whether the role was registered
        ///     or not.
        /// </returns>
        Task<bool> RegisterRole(string name, int displayIndex);
    }
}
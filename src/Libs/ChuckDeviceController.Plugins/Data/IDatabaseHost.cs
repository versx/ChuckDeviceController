namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Plugin host handler contract used to interact with the database entities.
    /// </summary>
    public interface IDatabaseHost
    {
        //IRepository<IAccount, string> Accounts { get; }
        //IRepository<IPokestop, string> Pokestops { get; }
        //IRepository<IDevice, string> Devices { get; }

        /// <summary>
        /// Gets a list of database entities.
        /// </summary>
        /// <typeparam name="T">Database entity contract type.</typeparam>
        /// <returns>Returns a list of database entities.</returns>
        Task<IReadOnlyList<T>> GetListAsync<T>();

        /// <summary>
        /// Gets a database entity by primary key.
        /// </summary>
        /// <typeparam name="T">Database entity contract type.</typeparam>
        /// <typeparam name="TId">Database entity primary key type.</typeparam>
        /// <param name="id">Primary key of the database entity.</param>
        /// <returns>Returns a database entity.</returns>
        Task<T?> GetByIdAsync<T, TId>(TId id);
    }
}
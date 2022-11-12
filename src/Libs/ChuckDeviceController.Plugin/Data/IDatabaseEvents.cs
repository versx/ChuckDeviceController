namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Provides delegates of database related events from
    /// the host application.
    /// </summary>
    public interface IDatabaseEvents
    {
        /// <summary>
        /// Called when the state of the database has changed.
        /// </summary>
        /// <param name="state">Current state of the database connection.</param>
        void OnStateChanged(DatabaseConnectionState state);

        /// <summary>
        /// Called when an entity has been added to the database by
        /// the host application.
        /// </summary>
        /// <typeparam name="T">Data entity type that was added.</typeparam>
        /// <param name="entity">The entity that was added.</param>
        void OnEntityAdded<T>(T entity);

        /// <summary>
        /// Called when an entity has been modified in the database by
        /// the host application.
        /// </summary>
        /// <typeparam name="T">Data entity type that was modified.</typeparam>
        /// <param name="oldEntity">The entity's previous version.</param>
        /// <param name="newEntity">The entity that was modified.</param>
        void OnEntityModified<T>(T oldEntity, T newEntity);

        /// <summary>
        /// Called when an entity has been deleted in the database by
        /// the host application.
        /// </summary>
        /// <typeparam name="T">Data entity type that was deleted.</typeparam>
        /// <param name="entity">The entity that was deleted.</param>
        void OnEntityDeleted<T>(T entity);
    }
}
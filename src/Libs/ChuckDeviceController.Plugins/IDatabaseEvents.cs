namespace ChuckDeviceController.Plugins
{
    public interface IDatabaseEvents
    {
        void OnStateChanged(DatabaseConnectionState state);

        void OnEntityAdded<T>(T entity);

        void OnEntityModified<T>(T oldEntity, T newEntity);

        void OnEntityDeleted<T>(T entity);
    }

    public enum DatabaseConnectionState
    {
        Connected,
        Disconnected,
    }
}
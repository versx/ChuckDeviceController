namespace ChuckDeviceController.Data.Interfaces
{
    public interface ISingleton<T>
    {
        static T Instance { get; }
    }
}
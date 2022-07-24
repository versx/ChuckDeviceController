namespace ChuckDeviceController.Services
{
    public interface IDataProcessorService
    {
        Task ConsumeDataAsync(string username, List<dynamic> data);
    }
}
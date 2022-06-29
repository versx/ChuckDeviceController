namespace ChuckDeviceController.Services
{
    public interface IDataProcessorService
    {
        Task ConsumeDataAsync(List<dynamic> data);
    }
}
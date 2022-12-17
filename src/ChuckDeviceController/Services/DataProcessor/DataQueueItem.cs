namespace ChuckDeviceController.Services.DataProcessor
{
    public class DataQueueItem
    {
        public string? Username { get; set; }

        public List<dynamic>? Data { get; set; }
    }
}
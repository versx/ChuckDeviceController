namespace ChuckDeviceConfigurator.ViewModels
{
    using ChuckDeviceController.Data.Entities;

    public class IvQueueViewModel
    {
        public string Name { get; set; }

        public List<Pokemon> Queue { get; set; } = new();
    }
}
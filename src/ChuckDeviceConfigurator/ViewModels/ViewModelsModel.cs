namespace ChuckDeviceConfigurator.ViewModels
{
    public class ViewModelsModel<T>
    {
        public bool AutoRefresh { get; set; }

        public List<T> Items { get; set; } = new();
    }
}
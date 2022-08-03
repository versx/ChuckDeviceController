namespace ChuckDeviceController.Plugins
{
    public interface IMetadata
    {
        string Name { get; }

        string Description { get; }

        string Author { get; }

        string Version { get; }
    }
}
namespace ChuckDeviceController.Plugins
{
    public interface IInstanceCreationOptions
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        List<string> Geofences { get; }

        string GroupName { get; }

        bool IsEvent { get; }
    }
}
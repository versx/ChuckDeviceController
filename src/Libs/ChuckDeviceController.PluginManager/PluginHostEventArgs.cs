namespace ChuckDeviceController.PluginManager;

using ChuckDeviceController.Common;

public sealed class PluginHostAddedEventArgs : EventArgs
{
    public IPluginHost PluginHost { get; }

    public PluginHostAddedEventArgs(IPluginHost pluginHost)
    {
        PluginHost = pluginHost;
    }
}

public sealed class PluginHostRemovedEventArgs : EventArgs
{
    public IPluginHost PluginHost { get; }

    public PluginHostRemovedEventArgs(IPluginHost pluginHost)
    {
        PluginHost = pluginHost;
    }
}

public sealed class PluginHostStateChangedEventArgs : EventArgs
{
    public IPluginHost PluginHost { get; }

    public PluginState PreviousState { get; }

    public PluginHostStateChangedEventArgs(IPluginHost pluginHost, PluginState previousState)
    {
        PluginHost = pluginHost;
        PreviousState = previousState;
    }
}
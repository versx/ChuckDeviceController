namespace ChuckDeviceController.JobControllers;

using ChuckDeviceController.Common;

public sealed class AutoInstanceCompleteEventArgs : EventArgs
{
    public string InstanceName { get; }

    public ulong CompletionTimestamp { get; }

    public AutoInstanceType InstanceType { get; }

    public AutoInstanceCompleteEventArgs(string instanceName, ulong completionTimestamp)
        : this(instanceName, completionTimestamp, AutoInstanceType.Quest)
    {
    }

    public AutoInstanceCompleteEventArgs(string instanceName, ulong completionTimestamp, AutoInstanceType instanceType = AutoInstanceType.Quest)
    {
        InstanceName = instanceName;
        CompletionTimestamp = completionTimestamp;
        InstanceType = instanceType;
    }
}
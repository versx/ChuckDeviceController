namespace ChuckDeviceConfigurator.JobControllers.EventArgs
{
    using System;

    public sealed class BootstrapInstanceCompleteEventArgs : EventArgs
    {
        public string InstanceName { get; }

        public string DeviceUuid { get; set; }

        public ulong CompletionTimestamp { get; }

        public BootstrapInstanceCompleteEventArgs(string instanceName, string deviceUuid, ulong completionTimestamp)
        {
            InstanceName = instanceName;
            DeviceUuid = deviceUuid;
            CompletionTimestamp = completionTimestamp;
        }
    }
}
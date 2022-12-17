namespace ChuckDeviceController.JobControllers.Models
{
    public class DeviceIndex
    {
        public int LastRouteIndex { get; set; }

        public ulong LastSeen { get; set; }

        public ulong LastCompleted { get; set; }

        // TODO: Actually implement this by checking if device has visited all coordinates, keep track of coordinates visited I suppose?
        public ulong LastCompletedWholeRoute { get; set; }

        public uint CoordinatesCompletedCount { get; set; }
    }
}
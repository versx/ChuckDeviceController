namespace ChuckDeviceController.JobControllers.Models
{
    public class DeviceIndex
    {
        public int LastRouteIndex { get; set; }

        public ulong LastSeen { get; set; }

        public ulong LastCompleted { get; set; }

        // TODO: Actually implement this by checking if device has visited all coordinates, keep track of coordinates visited I suppose?
        // TODO: Maybe add property for first coord it received and assume route is completed if last visited coord is first coord - 1
        public ulong LastCompletedWholeRoute { get; set; }

        public uint CoordinatesCompletedCount { get; set; }
    }
}
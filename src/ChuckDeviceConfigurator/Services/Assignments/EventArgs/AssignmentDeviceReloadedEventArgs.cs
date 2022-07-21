namespace ChuckDeviceConfigurator.Services.Assignments.EventArgs
{
    using System;

    using ChuckDeviceController.Data.Entities;

    public sealed class AssignmentDeviceReloadedEventArgs : EventArgs
    {
        public Device Device { get; }

        public AssignmentDeviceReloadedEventArgs(Device device)
        {
            Device = device;
        }
    }
}
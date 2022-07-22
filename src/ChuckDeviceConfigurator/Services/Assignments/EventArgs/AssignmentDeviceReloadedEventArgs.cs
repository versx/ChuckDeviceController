namespace ChuckDeviceConfigurator.Services.Assignments.EventArgs
{
    using System;

    using ChuckDeviceController.Data.Entities;

    /// <summary>
    /// Event arguments used when a device completes an assignment and needs to
    /// be reloaded in the <see cref="Jobs.JobControllerService"/>.
    /// </summary>
    public sealed class AssignmentDeviceReloadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the device that needs to be reloaded
        /// </summary>
        public Device Device { get; }

        /// <summary>
        /// Instantiates a new AssignmentDeviceReloadedEventArgs object.
        /// </summary>
        /// <param name="device">Device to be reloaded</param>
        public AssignmentDeviceReloadedEventArgs(Device device)
        {
            Device = device;
        }
    }
}
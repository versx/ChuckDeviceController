namespace ChuckDeviceConfigurator.Services.Assignments.EventArgs;

using System;

using ChuckDeviceController.Data.Entities;

public sealed class ReloadInstanceEventArgs : EventArgs
{
    public Instance Instance { get; }

    public ReloadInstanceEventArgs(Instance instance)
    {
        Instance = instance;
    }
}
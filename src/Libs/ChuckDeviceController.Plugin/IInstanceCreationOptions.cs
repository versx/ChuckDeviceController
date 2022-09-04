﻿namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data;

    /// <summary>
    /// 
    /// </summary>
    public interface IInstanceCreationOptions
    {
        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        InstanceType Type { get; }

        /// <summary>
        /// 
        /// </summary>
        ushort MinimumLevel { get; }

        /// <summary>
        /// 
        /// </summary>
        ushort MaximumLevel { get; }

        /// <summary>
        /// 
        /// </summary>
        List<string> Geofences { get; }

        /// <summary>
        /// 
        /// </summary>
        string GroupName { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsEvent { get; }
    }
}
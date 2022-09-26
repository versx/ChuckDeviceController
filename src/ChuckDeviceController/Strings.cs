﻿namespace ChuckDeviceController
{
    using System.Reflection;

    public static class Strings
    {
        private static readonly AssemblyName StrongAssemblyName = Assembly.GetExecutingAssembly().GetName();

        // File assembly details
        public static readonly string AssemblyName = StrongAssemblyName?.Name ?? "ChuckDeviceController";
        public static readonly string AssemblyVersion = StrongAssemblyName?.Version?.ToString() ?? "v1.0.0";
    }

    public class ProtoDataStatistics
    {
        #region Singleton

        private static ProtoDataStatistics? _instance;
        public static ProtoDataStatistics Instance => _instance ??= new();

        #endregion

        public ulong TotalRequestsProcessed { get; internal set; }

        public uint TotalProtoPayloadsReceived { get; internal set; }

        public uint TotalProtosProcessed { get; internal set; }

        public uint TotalEntitiesProcessed { get; internal set; }

        public uint TotalEntitiesUpserted { get; internal set; }
    }
}
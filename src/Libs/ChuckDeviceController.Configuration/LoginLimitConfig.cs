namespace ChuckDeviceController.Configuration;

public class LoginLimitConfig
{
    public bool Enabled { get; set; }

    public uint MaximumCount { get; set; }

    public ulong IntervalS { get; set; }
}
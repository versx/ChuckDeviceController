namespace ChuckDeviceController.JobControllers.Models;

internal class LevelingStatus
{
    public ulong XpTarget { get; set; }

    public ulong XpStart { get; set; }

    public ulong XpCurrent { get; set; }

    public double XpPercentage { get; set; }

    public ushort Level { get; set; }

    public string? Username { get; set; }

    public double XpPerHour { get; set; }

    public double TimeLeft { get; set; }
}
namespace ChuckDeviceConfigurator.Services.Rpc.Models
{
    public class TrainerLevelingStatus
    {
        public string? Username { get; internal set; }

        public bool StoreLevelingData { get; internal set; } = true;

        public bool IsTrainerLeveling { get; internal set; }
    }
}
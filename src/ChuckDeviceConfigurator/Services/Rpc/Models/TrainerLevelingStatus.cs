namespace ChuckDeviceConfigurator.Services.Rpc.Models
{
    public class TrainerLevelingStatus
    {
        public string? Username { get; }

        public bool StoreLevelingData { get; }

        public bool IsTrainerLeveling { get; }

        public TrainerLevelingStatus(string? username, bool storeLevelingData = false, bool isTrainerLeveling = false)
        {
            StoreLevelingData = storeLevelingData;
            IsTrainerLeveling = isTrainerLeveling;
            Username = username;
        }
    }
}
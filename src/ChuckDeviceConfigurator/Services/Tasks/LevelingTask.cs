namespace ChuckDeviceConfigurator.Services.Tasks
{
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common;

    public class LevelingTask : BaseJobTask
    {
        [JsonPropertyName("deploy_egg")]
        public bool DeployEgg { get; set; }

        [JsonPropertyName("delay")]
        public double Delay { get; set; }

        public LevelingTask()
        {
            Action = DeviceActionType.SpinPokestop;
        }
    }
}
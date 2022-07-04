namespace ChuckDeviceConfigurator.Services.Tasks
{
    using System.Text.Json.Serialization;

    public class QuestTask : BaseJobTask
    {
        [JsonPropertyName("delay")]
        public double Delay { get; set; }

        [JsonPropertyName("deploy_egg")]
        public bool DeployEgg { get; set; }

        [JsonPropertyName("quest_type")]
        public string QuestType { get; set; }

        public QuestTask()
        {
            Action = DeviceActionType.ScanQuest;
        }
    }
}
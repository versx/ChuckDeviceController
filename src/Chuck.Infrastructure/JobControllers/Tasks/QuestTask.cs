namespace Chuck.Infrastructure.JobControllers.Tasks
{
    using System.Text.Json.Serialization;

    public class QuestTask : ITask
    {
        [JsonPropertyName("area")]
        public string Area { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }

        [JsonPropertyName("delay")]
        public double Delay { get; set; }

        [JsonPropertyName("deploy_egg")]
        public bool DeployEgg { get; set; }

        public QuestTask()
        {
            Action = ActionType.ScanQuest;
        }
    }
}
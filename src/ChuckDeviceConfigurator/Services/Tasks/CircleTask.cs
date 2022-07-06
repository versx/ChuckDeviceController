namespace ChuckDeviceConfigurator.Services.Tasks
{
	using System.Text.Json.Serialization;

	public class CircleTask : BaseJobTask
	{
        [JsonPropertyName("lure_encounter")]    
		public bool LureEncounter { get; set; }}
}
namespace ChuckDeviceConfigurator.Services.Tasks
{
	using System.Text.Json.Serialization;

	using ChuckDeviceController.Common;

	public class CircleTask : BaseJobTask
	{
        [JsonPropertyName("lure_encounter")]
		public bool? LureEncounter { get; set; }

		public CircleTask()
        {
			Action = DeviceActionType.ScanPokemon;
        }
	}
}
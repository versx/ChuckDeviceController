namespace ChuckDeviceController.JobControllers.Tasks;

using System.Text.Json.Serialization;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Tasks;

public class CircleTask : BaseJobTask
{
    [JsonPropertyName("lure_encounter")]
	public bool? LureEncounter { get; set; }

	public CircleTask()
    {
	    Action = DeviceActionType.ScanPokemon;
    }
}
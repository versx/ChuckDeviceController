namespace ChuckDeviceConfigurator.Services.Tasks
{
	using System.Text.Json.Serialization;

	public class IvTask : BaseJobTask
	{
        [JsonPropertyName("id")]
		public ulong EncounterId { get; set; }

        [JsonPropertyName("is_spawnpoint")]
        public bool IsSpawnpoint { get; set; }

		public IvTask()
		{
			Action = DeviceActionType.ScanIV;
		}
	}
}
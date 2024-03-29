﻿namespace ChuckDeviceController.JobControllers.Tasks;

using System.Text.Json.Serialization;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Tasks;

public class IvTask : BaseJobTask
{
    [JsonPropertyName("id")]
	public string? EncounterId { get; set; }

    [JsonPropertyName("is_spawnpoint")]
    public bool IsSpawnpoint { get; set; }

    [JsonPropertyName("lure_encounter")]
	public bool? LureEncounter { get; set; }

	public IvTask()
	{
		Action = DeviceActionType.ScanIV;
	}
}
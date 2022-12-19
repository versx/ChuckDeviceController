namespace ChuckDeviceController.Services.ProtoProcessor;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Net.Models.Requests;

public class ProtoPayloadQueueItem
{
    public ProtoPayload? Payload { get; set; }

    public Device? Device { get; set; }
}
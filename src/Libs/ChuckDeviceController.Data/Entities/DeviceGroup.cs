namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ChuckDeviceController.Common.Abstractions;

[Table("device_group")]
public class DeviceGroup : BaseEntity, IDeviceGroup
{
    [
        DisplayName("Name"),
        Column("name"),
        Key,
    ]
    public string Name { get; set; } = null!;

    [
        DisplayName("Devices"),
        Column("device_uuids"),
    ]
    public List<string> DeviceUuids { get; set; } = new();
}
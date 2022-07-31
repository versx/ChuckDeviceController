namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("device_group")]
    public class DeviceGroup : BaseEntity
    {
        [
            DisplayName("Name"),
            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [
            DisplayName("Devices"),
            Column("device_uuids"),
        ]
        public List<string> DeviceUuids { get; set; }
    }
}
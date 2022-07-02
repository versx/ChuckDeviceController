namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("assignment")]
    public class Assignment : BaseEntity
    {
        [

            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.Identity),
        ]
        public uint Id { get; set; }

        [
            DisplayName("Instance Name"),
            Column("instance_name"),
        ]
        public string InstanceName { get; set; }

        [
            DisplayName("Source Instance Name"),
            Column("source_instance_name"),
        ]
        public string? SourceInstanceName { get; set; }

        [
            DisplayName("Device Uuid"),
            Column("device_uuid"),
         ]
        public string? DeviceUuid { get; set; }

        [
            DisplayName("Time"),
            Column("time"),
        ]
        public uint Time { get; set; }

        [
            DisplayName("Date"),
            Column("date"),
            DataType(DataType.Date),
        ]
        public DateTime? Date { get; set; }

        [
            DisplayName("Device Group"),
            Column("device_group_name"),
        ]
        public string? DeviceGroupName { get; set; }

        [
            DisplayName("Enabled"),
            Column("enabled"),
        ]
        public bool Enabled { get; set; }
    }
}
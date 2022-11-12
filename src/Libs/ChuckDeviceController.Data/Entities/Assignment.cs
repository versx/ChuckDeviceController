namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Common.Data.Contracts;

    [Table("assignment")]
    public class Assignment : BaseEntity, IAssignment
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
            DisplayName("Device"),
            Column("device_uuid"),
        ]
        public string? DeviceUuid { get; set; }

        [
            DisplayName("Time"),
            Column("time"),
        ]
        public uint Time { get; set; }

        [
            DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true),
            DisplayName("Date"),
            Column("date"),
            DataType(DataType.Date),
            DefaultValue(null),
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
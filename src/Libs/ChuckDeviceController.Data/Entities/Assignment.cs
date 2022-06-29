namespace ChuckDeviceController.Data.Entities
{
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

        [Column("instance_name")]
        public string InstanceName { get; set; }

        [Column("source_instance_name")]
        public string SourceInstanceName { get; set; }

        [Column("device_uuid")]
        public string DeviceUuid { get; set; }

        [Column("time")]
        public uint Time { get; set; }

        [

            Column("date"),
            DataType(DataType.Date),
        ]
        public DateTime? Date { get; set; }

        [Column("device_group_name")]
        public string DeviceGroupName { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }
    }
}
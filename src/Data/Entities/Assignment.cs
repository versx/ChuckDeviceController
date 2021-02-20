namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Data.Interfaces;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("assignment")]
    public class Assignment : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key
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

        // TODO: device_group_name

        [
            Column("enabled"),
        ]
        public bool Enabled { get; set; }
    }
}
namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Chuck.Infrastructure.Data.Interfaces;

    [Table("device_group")]
    public class DeviceGroup : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [Column("devices")]
        public List<string> Devices { get; set; }

        //public virtual ICollection<DeviceGroupDevice> Devices { get; set; }
    }

    /*
    [Table("device_group_device")]
    public class DeviceGroupDevice
    {
        [
            Column("device_group_name"),
            Key,
        ]
        public string DeviceGroupName { get; set; }

        [Column("device_uuid")]
        public string DeviceUuid { get; set; }
    }
    */
}
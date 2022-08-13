namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Common.Data;

    [Table("plugin")]
    public class Plugin : BaseEntity
    {
        [
            Column("name"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Name { get; set; }

        [Column("state")]
        public PluginState State { get; set; }

        // TODO: RequestedPermissions
        // TODO: AllowedPermissions
    }
}
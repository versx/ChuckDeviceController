namespace Chuck.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Chuck.Data.Interfaces;

    [Table("user")]
    public class User : BaseEntity, IAggregateRoot
    {
        [
            Column("username"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("username"),
        ]
        public string Username { get; set; }

        [
            Column("password"),
            JsonPropertyName("password"),
        ]
        public string Password { get; set; }

        [
            Column("permissions"),
            JsonPropertyName("permissions"),
        ]
        public Permission Permissions { get; set; }

        [
            Column("created"),
            JsonPropertyName("created"),
        ]
        public ulong Created { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        [
            Column("enabled"),
            JsonPropertyName("enabled"),
        ]
        public bool Enabled { get; set; }


        public bool HasPermission(Permission perm)
        {
            return (Permissions & perm) == perm;
        }

        public void AddPermission(Permission perm)
        {
            Permissions |= perm;
        }

        public void RemovePermission(Permission perm)
        {
            Permissions &= (~perm);
        }
    }

    [Flags]
    public enum Permission
    {
        None = 0x0,
        View = 0x1,
        Devices = 0x2,
        DeviceGroups = 0x4,
        Assignments = 0x8,
        Instances = 0x10,
        Geofences = 0x20,
        IVLists = 0x40,
        Webhooks = 0x80,
        Accounts = 0x100,
        Utilities = 0x200,
        Settings = 0x400,
        Reserved = 0x800,
        Admin = 0x1000000,
    }
}
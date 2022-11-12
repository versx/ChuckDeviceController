﻿namespace ChuckDeviceConfigurator.Data
{
    using System.ComponentModel.DataAnnotations.Schema;

    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        [Column("username_change_limit")]
        public int UsernameChangeLimit { get; set; } = 10;

        [Column("profile_picture")]
        public byte[]? ProfilePicture { get; set; }
    }
}
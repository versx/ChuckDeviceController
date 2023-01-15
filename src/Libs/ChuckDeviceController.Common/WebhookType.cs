namespace ChuckDeviceController.Common;

using System.ComponentModel.DataAnnotations;

[Flags]
public enum WebhookType : int
{
    [Display(GroupName = "", Name = "None", Description = "")]
    None = 0,

    [Display(GroupName = "Pokemon", Name = "Pokemon", Description = "")]
    Pokemon = 1 << 0,

    [Display(GroupName = "Pokestops", Name = "Pokestops", Description = "")]
    Pokestops = 1 << 1,

    [Display(GroupName = "Pokestops", Name = "Lures", Description = "")]
    Lures = 1 << 2,

    [Display(GroupName = "Pokestops", Name = "Invasions", Description = "")]
    Invasions = 1 << 3,

    [Display(GroupName = "Pokestops", Name = "Quests", Description = "")]
    Quests = 1 << 4,

    [Display(GroupName = "Pokestops", Name = "AlternativeQuests", Description = "")]
    AlternativeQuests = 1 << 5,

    [Display(GroupName = "Gyms", Name = "Gyms", Description = "")]
    Gyms = 1 << 6,

    [Display(GroupName = "Gyms", Name = "GymInfo", Description = "")]
    GymInfo = 1 << 7,

    [Display(GroupName = "Gyms", Name = "GymDefenders", Description = "")]
    GymDefenders = 1 << 8,

    [Display(GroupName = "Gyms", Name = "GymTrainers", Description = "")]
    GymTrainers = 1 << 9,

    [Display(GroupName = "Raids", Name = "Eggs", Description = "")]
    Eggs = 1 << 10,

    [Display(GroupName = "Raids", Name = "Raids", Description = "")]
    Raids = 1 << 11,

    [Display(GroupName = "Other", Name = "Weather", Description = "")]
    Weather = 1 << 12,

    [Display(GroupName = "Other", Name = "Accounts", Description = "")]
    Accounts = 1 << 13,

    //[Display(GroupName = "", Name = "All", Description = "")]
    //All = // ~(~0 << 14),
    //    Pokemon |
    //    Pokestops |
    //    Lures |
    //    Invasions |
    //    Quests |
    //    AlternativeQuests |
    //    Gyms |
    //    GymInfo |
    //    GymDefenders |
    //    GymTrainers |
    //    Eggs |
    //    Raids |
    //    Weather |
    //    Accounts,
}
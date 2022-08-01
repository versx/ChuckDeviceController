namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    using ChuckDeviceController.Data.Entities;

    public class AccountStatisticsViewModel
    {
        public List<Account> Accounts { get; set; } = new();

        [
            DisplayName("New"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong NewAccounts { get; set; }

        [
            DisplayName("In Use"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong AccountsInUse { get; set; }

        [
            DisplayName("Clean Level 30"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong CleanLevel30s { get; set; }

        [
            DisplayName("Level 30+"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong OverLevel30 { get; set; }

        [
            DisplayName("Suspended"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong SuspendedAccounts { get; set; }

        [
            DisplayName("Total"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong TotalAccounts { get; set; }

        [
            DisplayName("In Cooldown"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong InCooldown { get; set; }

        [
            DisplayName("Over Spin Limit"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong OverSpinLimit { get; set; }

        public AccountWarningsBansViewModel Bans { get; set; } = new();

        public AccountWarningsBansViewModel Warnings { get; set; } = new();

        public AccountWarningsBansViewModel Suspended { get; set; } = new();

        public List<AccountLevelStatisticsViewModel> AccountLevelStatistics { get; set; } = new();
    }

    public class AccountWarningsBansViewModel
    {
        [
            DisplayName("Last 24 Hours"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Last24Hours { get; set; }

        [
            DisplayName("Last 7 Days"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Last7Days { get; set; }

        [
            DisplayName("Last 30 Days"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Last30Days { get; set; }

        [
            DisplayName("Total"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Total { get; set; }
    }

    public class AccountLevelStatisticsViewModel
    {
        [DisplayName("Level")]
        public ushort Level { get; set; }

        [
            DisplayName("Total"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Total { get; set; }

        [
            DisplayName("In Use"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong InUse { get; set; }

        [
            DisplayName("Good"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Good { get; set; }

        [
            DisplayName("Banned"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Banned { get; set; }

        [
            DisplayName("Warning"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Warning { get; set; }

        [
            DisplayName("Suspended"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Suspended { get; set; }

        [
            DisplayName("Invalid"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Invalid { get; set; }

        [
            DisplayName("Cooldown"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Cooldown { get; set; }

        [
            DisplayName("Spin Limit"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong SpinLimit { get; set; }

        [
            DisplayName("Other"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Other { get; set; }
    }
}
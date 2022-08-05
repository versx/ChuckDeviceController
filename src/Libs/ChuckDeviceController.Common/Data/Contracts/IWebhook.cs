namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IWebhook : IBaseEntity
    {
        string Name { get; }

        IList<WebhookType> Types { get; }

        double Delay { get; }

        string Url { get; }

        bool Enabled { get; }

        IList<string> Geofences { get; }

        IWebhookData Data { get; }
    }

    public interface IWebhookData
    {
        IList<uint> PokemonIds { get; }

        IList<string> PokestopIds { get; }

        IList<uint> RaidPokemonIds { get; }

        IList<ushort> EggLevels { get; }

        IList<ushort> LureIds { get; }

        IList<ushort> InvasionIds { get; }

        IList<ushort> GymTeamIds { get; }

        IList<ushort> WeatherConditionIds { get; }
    }
}
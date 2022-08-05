namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IWebhook : IBaseEntity
    {
        string Name { get; }

        List<WebhookType> Types { get; }

        double Delay { get; }

        string Url { get; }

        bool Enabled { get; }

        List<string> Geofences { get; }

        // TODO: IWebhookData Data { get; }
    }

    public interface IWebhookData
    {
        List<uint> PokemonIds { get; }

        List<string> PokestopIds { get; }

        List<uint> RaidPokemonIds { get; }

        List<ushort> EggLevels { get; }

        List<ushort> LureIds { get; }

        List<ushort> InvasionIds { get; }

        List<ushort> GymTeamIds { get; }

        List<ushort> WeatherConditionIds { get; }
    }
}
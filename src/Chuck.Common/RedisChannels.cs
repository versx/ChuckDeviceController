namespace Chuck.Common
{
    public static class RedisChannels
    {
        // Proto channels
        public const string ProtoWildPokemon = "proto:wild_pokemon";
        public const string ProtoNearbyPokemon = "proto:nearby_pokemon";
        public const string ProtoEncounter = "proto:encounter";
        public const string ProtoFort = "proto:fort";
        public const string ProtoGymInfo = "proto:gym_info";
        public const string ProtoGymDefender = "proto:gym_defender";
        public const string ProtoGymTrainer = "proto:gym_trainer";
        public const string ProtoQuest = "proto:quest";
        public const string ProtoCell = "proto:cell";
        public const string ProtoWeather = "proto:weather";
        public const string ProtoAccount = "proto:account";

        // Webhook channels
        public const string WebhookPokemon = "webhook:pokemon";
        public const string WebhookGym = "webhook:gym";
        public const string WebhookRaid = "webhook:raid";
        public const string WebhookEgg = "webhook:egg";
        public const string WebhookLure = "webhook:lure";
        public const string WebhookInvasion = "webhook:invasion";
        public const string WebhookPokestop = "webhook:pokestop";
        public const string WebhookGymDefender = "webhook:gym_defender";
        public const string WebhookGymTrainer = "webhook:gym_trainer";
        public const string WebhookQuest = "webhook:quest";
        //public const string WebhookCell = "webhook:cell";
        public const string WebhookWeather = "webhook:weather";
        public const string WebhookAccount = "webhook:account";
        public const string WebhookReload = "webhook:reload";

        public const string PokemonAdded = "pokemon:added";
        public const string PokemonUpdated = "pokemon:updated";
    }
}
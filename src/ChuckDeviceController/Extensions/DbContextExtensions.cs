namespace ChuckDeviceController.Extensions
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;

    public static class DbContextExtensions
    {
        public static void UpdatePokestopProperties<T>(this T context, Pokestop pokestop, bool updateQuests = false)
            where T : DbContext
        {
            context.Attach(pokestop);
            context.Entry(pokestop).Property(p => p.Name).IsModified = true;
            context.Entry(pokestop).Property(p => p.Url).IsModified = true;
            context.Entry(pokestop).Property(p => p.LureId).IsModified = true;
            context.Entry(pokestop).Property(p => p.LureExpireTimestamp).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpLevel).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpPoints).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpEndTimestamp).IsModified = true;

            if (updateQuests)
            {
                context.Entry(pokestop).Property(p => p.QuestConditions).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestItemId).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestPokemonId).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewardAmount).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewards).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewardType).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTarget).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTemplate).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTimestamp).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTitle).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestType).IsModified = true;

                context.Entry(pokestop).Property(p => p.AlternativeQuestConditions).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestItemId).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestPokemonId).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewardAmount).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewards).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewardType).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTarget).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTemplate).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTimestamp).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTitle).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestType).IsModified = true;
            }
        }

        public static void UpdateGymProperties<T>(this T context, Pokestop pokestop, bool updateQuests = false)
            where T : DbContext
        {
            context.Attach(pokestop);
            context.Entry(pokestop).Property(p => p.Name).IsModified = true;
            context.Entry(pokestop).Property(p => p.Url).IsModified = true;
            context.Entry(pokestop).Property(p => p.LureId).IsModified = true;
            context.Entry(pokestop).Property(p => p.LureExpireTimestamp).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpLevel).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpPoints).IsModified = true;
            context.Entry(pokestop).Property(p => p.PowerUpEndTimestamp).IsModified = true;

            if (updateQuests)
            {
                context.Entry(pokestop).Property(p => p.QuestConditions).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestItemId).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestPokemonId).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewardAmount).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewards).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestRewardType).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTarget).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTemplate).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTimestamp).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestTitle).IsModified = true;
                context.Entry(pokestop).Property(p => p.QuestType).IsModified = true;

                context.Entry(pokestop).Property(p => p.AlternativeQuestConditions).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestItemId).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestPokemonId).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewardAmount).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewards).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestRewardType).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTarget).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTemplate).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTimestamp).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestTitle).IsModified = true;
                context.Entry(pokestop).Property(p => p.AlternativeQuestType).IsModified = true;
            }
        }

        public static void UpdatePokemonProperties<T>(this T context, Pokemon pokemon, bool updateIv = false)
            where T : DbContext
        {
            context.Attach(pokemon);
            context.Entry(pokemon).Property(p => p.PokemonId).IsModified = true;
            context.Entry(pokemon).Property(p => p.Form).IsModified = true;
            context.Entry(pokemon).Property(p => p.Costume).IsModified = true;
            context.Entry(pokemon).Property(p => p.DisplayPokemonId).IsModified = true;
            context.Entry(pokemon).Property(p => p.SeenType).IsModified = true;

            context.Entry(pokemon).Property(p => p.ExpireTimestamp).IsModified = true;
            context.Entry(pokemon).Property(p => p.IsExpireTimestampVerified).IsModified = true;
            context.Entry(pokemon).Property(p => p.Changed).IsModified = true;
            context.Entry(pokemon).Property(p => p.Updated).IsModified = true;

            if (updateIv)
            {
                context.Entry(pokemon).Property(p => p.AttackIV).IsModified = true;
                context.Entry(pokemon).Property(p => p.DefenseIV).IsModified = true;
                context.Entry(pokemon).Property(p => p.StaminaIV).IsModified = true;
                context.Entry(pokemon).Property(p => p.CP).IsModified = true;
                context.Entry(pokemon).Property(p => p.Level).IsModified = true;
                context.Entry(pokemon).Property(p => p.Gender).IsModified = true;
                context.Entry(pokemon).Property(p => p.Move1).IsModified = true;
                context.Entry(pokemon).Property(p => p.Move2).IsModified = true;
                context.Entry(pokemon).Property(p => p.Size).IsModified = true;
                context.Entry(pokemon).Property(p => p.Weight).IsModified = true;
                context.Entry(pokemon).Property(p => p.Weather).IsModified = true;

                //context.Entry(pokemon).Property(p => p.PvpRankings).IsModified = true;

                context.Entry(pokemon).Property(p => p.BaseHeight).IsModified = true;
                context.Entry(pokemon).Property(p => p.BaseWeight).IsModified = true;

                context.Entry(pokemon).Property(p => p.Capture1).IsModified = true;
                context.Entry(pokemon).Property(p => p.Capture2).IsModified = true;
                context.Entry(pokemon).Property(p => p.Capture3).IsModified = true;
            }
        }
    }
}
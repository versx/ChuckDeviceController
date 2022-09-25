namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IPokestop : IBaseEntity
    {
        string Id { get; }

        double Latitude { get; }

        double Longitude { get; }

        string? Name { get; }

        string? Url { get; }

        ushort LureId { get; }

        ulong? LureExpireTimestamp { get; }

        ulong LastModifiedTimestamp { get; }

        ulong Updated { get; }

        bool IsEnabled { get; }

        ulong CellId { get; }

        bool IsDeleted { get; }

        ulong FirstSeenTimestamp { get; }

        uint? SponsorId { get; }

        bool IsArScanEligible { get; }

        uint? PowerUpPoints { get; }

        ushort? PowerUpLevel { get; }

        ulong? PowerUpEndTimestamp { get; }

        #region Quests

        uint? QuestType { get; }

        string? QuestTemplate { get; }

        string? QuestTitle { get; }

        ushort? QuestTarget { get; }

        ulong? QuestTimestamp { get; }

        #region Virtual Columns

        ushort? QuestRewardType { get; }

        ushort? QuestItemId { get; }

        ushort? QuestRewardAmount { get; }

        uint? QuestPokemonId { get; }

        #endregion

        List<Dictionary<string, dynamic>>? QuestConditions { get; }

        List<Dictionary<string, dynamic>>? QuestRewards { get; }

        uint? AlternativeQuestType { get; }

        string? AlternativeQuestTemplate { get; }

        string? AlternativeQuestTitle { get; }

        ushort? AlternativeQuestTarget { get; }

        ulong? AlternativeQuestTimestamp { get; }

        #region Virtual Columns

        ushort? AlternativeQuestRewardType { get; }

        ushort? AlternativeQuestItemId { get; }

        ushort? AlternativeQuestRewardAmount { get; }

        uint? AlternativeQuestPokemonId { get; }

        #endregion

        List<Dictionary<string, dynamic>>? AlternativeQuestConditions { get; }

        List<Dictionary<string, dynamic>>? AlternativeQuestRewards { get; }

        #endregion
    }
}
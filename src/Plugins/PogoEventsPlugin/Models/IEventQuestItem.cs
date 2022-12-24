namespace PogoEventsPlugin.Models;

public interface IEventQuestItem
{
    string Task { get; }

    List<EventQuestReward> Rewards { get; }
}

public interface IEventQuestReward
{
    string Type { get; }

    uint? Amount { get; }

    uint? Id { get; }

    EventQuestRewardItem? Reward { get; }
}

public interface IEventQuestRewardItem : IEventItem
{
    uint? Form { get; }
}
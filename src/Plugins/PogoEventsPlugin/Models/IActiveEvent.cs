namespace PogoEventsPlugin.Models
{
    public interface IActiveEvent
    {
        string Name { get; }

        string Type { get; } // event type: spotlight-hour, raid-hour, event, community-day, season

        string Start { get; }

        string End { get; }

        IEnumerable<EventItem> Spawns { get; } // id, template

        IEnumerable<EventItem> Eggs { get; } // id, template

        IEnumerable<EventRaidItem> Raids { get; } // id, template

        IEnumerable<EventItem> Shinies { get; } // ?

        IEnumerable<EventBonusItem> Bonuses { get; } // ?

        IEnumerable<string> Features { get; } // ?

        bool HasQuests { get; } // has_quests

        bool HasSpawnpoints { get; } // has_spawnpoints

        bool IsActive { get; }
    }
}
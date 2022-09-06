namespace PogoEventsPlugin.Models
{
    public interface IActiveEvent
    {
        string Name { get; }

        string Type { get; } // event type: spotlight-hour, raid-hour, event, community-day, season

        string Start { get; }

        string End { get; }

        IEnumerable<ActiveEventItem> Spawns { get; } // id, template

        IEnumerable<ActiveEventItem> Eggs { get; } // id, template

        IEnumerable<ActiveEventRaidItem> Raids { get; } // id, template

        IEnumerable<ActiveEventItem> Shinies { get; } // ?

        IEnumerable<BonusItem> Bonuses { get; } // ?

        IEnumerable<ActiveEventItem> Features { get; } // ?

        bool HasQuests { get; } // has_quests

        bool HasSpawnpoints { get; } // has_spawnpoints
    }
}
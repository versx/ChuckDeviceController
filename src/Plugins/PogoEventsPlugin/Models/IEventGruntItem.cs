namespace PogoEventsPlugin.Models;

public interface IEventGruntItem
{
    bool Active { get; }

    EventGruntCharacter Character { get; }

    EventGruntLineUp? Lineup { get; }
}

public interface IEventGruntCharacter
{
    string? Template { get; }

    ushort Gender { get; }

    EventGruntCharacterType Type { get; }

    bool Boss { get; }
}

public interface IEventGruntCharacterType
{
    uint Id { get; }

    string Name { get; }
}

public interface IEventGruntLineUp
{
    List<uint> Rewards { get; }

    List<List<EventGruntLineUpTeam>> Team { get; }
}

public interface IEventGruntLineUpTeam
{
    uint Id { get; }

    string? Template { get; }

    uint Form { get; }
}
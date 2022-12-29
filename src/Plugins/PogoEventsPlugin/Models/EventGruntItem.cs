namespace PogoEventsPlugin.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class EventGruntItem : IEventGruntItem
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("character")]
    public EventGruntCharacter Character { get; set; } = null!;

    [JsonPropertyName("lineup")]
    public EventGruntLineUp? Lineup { get; set; }
}

public class EventGruntCharacter : IEventGruntCharacter
{
    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("gender")]
    public ushort Gender { get; set; }

    [JsonPropertyName("type")]
    public EventGruntCharacterType Type { get; set; } = new();

    [JsonPropertyName("boss")]
    public bool Boss { get; set; }
}

public class EventGruntCharacterType : IEventGruntCharacterType
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

public class EventGruntLineUp : IEventGruntLineUp
{
    [JsonPropertyName("team")]
    public List<List<EventGruntLineUpTeam>> Team { get; set; } = new();

    [JsonPropertyName("rewards")]
    public List<uint> Rewards { get; set; } = new();
}

public class EventGruntLineUpTeam : IEventGruntLineUpTeam
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("form")]
    public uint Form { get; set; }
}
namespace TodoPlugin.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(TodoPlugin.DbName)]
public class Todo
{
    [
        DisplayName("ID"),
        Column("id"),
        Key,
    ]
    public uint Id { get; set; }

    [
        DisplayName("Name"),
        Column("name"),
    ]
    public string? Name { get; set; }

    [
        DisplayName("Is Complete"),
        Column("is_complete"),
    ]
    public bool IsComplete { get; set; }
}
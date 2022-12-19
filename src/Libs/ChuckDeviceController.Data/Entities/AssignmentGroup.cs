namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ChuckDeviceController.Data.Abstractions;

[Table("assignment_group")]
public class AssignmentGroup : BaseEntity, IAssignmentGroup
{
    [
        DisplayName("Name"),
        Column("name"),
        Key,
    ]
    public string Name { get; set; }

    [
        DisplayName("Assignments"),
        Column("assignment_ids"),
    ]
    public List<uint> AssignmentIds { get; set; }
}
namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("assignment_group")]
    public class AssignmentGroup : BaseEntity
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
}
namespace Chuck.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Chuck.Data.Interfaces;

    [Table("assignment_group")]
    public class AssignmentGroup : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [Column("assignment_ids")]
        public List<uint> AssignmentIds { get; set; }
    }
}
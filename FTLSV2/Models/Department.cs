using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FTLSV2.Models
{
    [Table("departments")]
    public class Department
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("school_id")]
        public int SchoolId { get; set; }

        [Column("code")]
        public string? Code { get; set; }

        [Column("department")]
        public string? Name { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SchoolId))]
        public School? School { get; set; }
        // <-- Added [JsonIgnore] right her
        [JsonIgnore]
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}

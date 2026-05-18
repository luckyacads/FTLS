using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
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

        // Navigation property mapping back to any subjects belonging here
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
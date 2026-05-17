using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    public class Curriculum
    {
        [Key]
        [Column("curriculum_id")]
        public int CurriculumId { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("department")]
        public string Department { get; set; }

        [Column("year_level")]
        public string YearLevel { get; set; }

        [Column("semester")]
        public string Semester { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

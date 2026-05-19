using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("curriculum")]
    public class Curriculum
    {
        [Key]
        [Column("curriculum_id")]
        public int CurriculumId { get; set; }

        // NOT CONNECTED IN NEONDB: Just stores the integer.
        [Column("subject_id")]
        public int SubjectId { get; set; }

        // [NotMapped] tells the database to completely ignore this connection
        [NotMapped]
        public virtual Subject Subject { get; set; }

        // Added the actual integer ID column
        [Column("department_id")]
        public int DepartmentId { get; set; }

        // Renamed from 'Departments' to 'Department' so the .Include() works!
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [Column("year_level")]
        public string YearLevel { get; set; }

        [Column("semester")]
        public string Semester { get; set; }

      //  [Column("created_at")]
       // public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //[Column("updated_at")]
       // public DateTime? UpdatedAt { get; set; }
    }
}
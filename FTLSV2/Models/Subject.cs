using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    public class Subject
    {
        [Key]
        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("subject_code")]
        public string Code { get; set; }

        [Column("subject_title")]
        public string Title { get; set; }

        [Column("units")]
        public int Units { get; set; }

        // CHANGED: Made DepartmentId nullable by adding a question mark
        [Column("department_id")]
        public int? DepartmentId { get; set; }

        // CHANGED: Made the Department object nullable
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }
    }
}
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

        [Column("department")]
        public string Department { get; set; }

        [Column("semester")]
        public string Semester { get; set; }
    }
}

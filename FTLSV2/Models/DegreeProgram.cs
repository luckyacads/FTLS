using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("degree_programs")]
    public class DegreeProgram
    {
        [Key]
        [Column("program_id")]
        public int ProgramId { get; set; }

        [Column("program_code")]
        public string Code { get; set; }

        [Column("program_name")]
        public string Name { get; set; }

        [Column("department_id")]
        public int DepartmentId { get; set; }
    }
}
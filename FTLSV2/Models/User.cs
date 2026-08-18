using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace FTLSV2.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("faculty_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int FacultyId { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("max_units")]
        public int MaxUnits { get; set; }

        [Column("faculty_type")]
        [StringLength(20)]
        public string? FacultyType { get; set; }

        [Column("school_id")]
        public int? SchoolId { get; set; }

        // ---> ADDED: School Navigation Property
        [ForeignKey(nameof(SchoolId))]
        public School? School { get; set; }

        [Column("department_id")]
        public int? DepartmentId { get; set; }

        // ---> ADDED: Department Navigation Property
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Column("current_units")]
        public int CurrentUnits { get; set; }

        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---> ADDED: Soft Delete Flag
        [Column("is_delete")]
        public bool Is_delete { get; set; } = false;
    }
}
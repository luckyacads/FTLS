using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public DateTime Timestamp { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Action { get; set; }

        // Navigation property to User
        public virtual User User { get; set; }
    }
}

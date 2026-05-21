using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("schools")]
    public class School
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("created_at")]
        // ---> FIXED: Added auto-timestamp to prevent -Infinity issues on creation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
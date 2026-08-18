using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("schedule_slot")]
    public class ScheduleSlot
    {
        [Key]
        [Column("schedule_slot_id")]
        public int ScheduleSlotId { get; set; }

        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("day")]
        [StringLength(2)]
        public string Day { get; set; } = string.Empty;

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [ForeignKey(nameof(ScheduleId))]
        public Schedule Schedule { get; set; } = null!;
    }
}
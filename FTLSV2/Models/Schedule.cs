using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("schedules")]
    public class Schedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("faculty_id")]
        public string FacultyId { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("time_slot")]
        public string TimeSlot { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
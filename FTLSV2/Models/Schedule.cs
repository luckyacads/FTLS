using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTLSV2.Models
{
    [Table("schedule")]
    public class Schedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        // FIXED: Changed to int to perfectly match the User model
        [Column("faculty_id")]
        public int FacultyId { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("room_id")]
        public int? RoomId { get; set; }

        [Column("time_slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [Column("assigned_units")]
        public int AssignedUnits { get; set; }

        [Column("offer_code")]
        public int? OfferCode { get; set; }

        [Column("academic_year")]
        public string? AcademicYear { get; set; }

        [Column("semester")]
        public string? Semester { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public User Faculty { get; set; } = null!;

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }

        public ICollection<ScheduleSlot> ScheduleSlots { get; set; }
        = new List<ScheduleSlot>();
    }

    
}
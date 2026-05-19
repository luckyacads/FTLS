using System;
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

        // FK to users.id
        [Column("faculty_id")]
        public int FacultyId { get; set; }

        // FK to subject.subject_id
        [Column("subject_id")]
        public int SubjectId { get; set; }

        // FK to room_registry.room_id
        [Column("room_id")]
        public int? RoomId { get; set; }

        // stored as varchar in DB (user-provided timeslot string)
        [Column("time_slot")]
        public string TimeSlot { get; set; }

        [Column("assigned_units")]
        public int AssignedUnits { get; set; }

        [Column("offer_code")]
        public int? OfferCode { get; set; }

        [Column("academic_year")]
        public string? AcademicYear { get; set; }

        [Column("semester")]
        public string? Semester { get; set; }

        // Navigation properties
        [ForeignKey(nameof(FacultyId))]
        public User Faculty { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room Room { get; set; }
    }
}
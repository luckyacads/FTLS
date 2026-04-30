using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;

namespace FTLSV2.Pages.Chairman
{
    public class MyScheduleModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public MyScheduleModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record ScheduleView(string TimeSlot, string SubjectCode, string RoomName, string Units);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();

        public void OnGet()
        {
            // Get the currently logged-in chairman's ID from session (this is the faculty_id / user id)
            var chairmanIdStr = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(chairmanIdStr) || !int.TryParse(chairmanIdStr, out int chairmanId))
            {
                // Not logged in or invalid ID, return empty
                return;
            }

            // Get all schedules for this faculty member directly from schedule table
            var schedules = _db.Schedules
                .Where(s => s.FacultyId == chairmanId)
                .AsNoTracking()
                .ToList();

            if (!schedules.Any())
            {
                return;
            }

            // Get related room and subject data
            var roomIds = schedules.Select(s => s.RoomId).Distinct().ToList();
            var subjectIds = schedules.Select(s => s.SubjectId).Distinct().ToList();

            var rooms = _db.Rooms
                .Where(r => roomIds.Contains(r.RoomId))
                .AsNoTracking()
                .ToDictionary(r => r.RoomId);

            var subjects = _db.Subjects
                .Where(s => subjectIds.Contains(s.SubjectId))
                .AsNoTracking()
                .ToDictionary(s => s.SubjectId);

            // Separate schedules by day
            MWFSchedules = schedules
                .Where(s => !string.IsNullOrEmpty(s.TimeSlot) && s.TimeSlot.StartsWith("MWF"))
                .Select(s => new ScheduleView(
                    s.TimeSlot,
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                    rooms.ContainsKey(s.RoomId) ? rooms[s.RoomId].Name : "TBA",
                    s.AssignedUnits.ToString()
                ))
                .OrderBy(s => s.TimeSlot)
                .ToList();

            TTHSchedules = schedules
                .Where(s => !string.IsNullOrEmpty(s.TimeSlot) && (s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")))
                .Select(s => new ScheduleView(
                    s.TimeSlot,
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                    rooms.ContainsKey(s.RoomId) ? rooms[s.RoomId].Name : "TBA",
                    s.AssignedUnits.ToString()
                ))
                .OrderBy(s => s.TimeSlot)
                .ToList();
        }
    }
}

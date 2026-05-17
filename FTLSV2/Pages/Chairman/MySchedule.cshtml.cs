using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTLSV2.Pages.Chairman
{
    public class MyScheduleModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public MyScheduleModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record ScheduleView(string TimeSlot, string SubjectCode, string SubjectTitle, string RoomName, string Units, int? OfferCode);

        // UPGRADED TO MATCH TEACHER PORTAL
        public record SubjectGroup(string HeaderName, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();

        // UPGRADED TO MATCH TEACHER PORTAL
        public List<SubjectGroup> SubjectHistory { get; set; } = new();
        public List<string> AvailableYears { get; set; } = new();

        // Summary statistics
        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedYear { get; set; }

        public void OnGet()
        {
            // Get the currently logged-in chairman's faculty ID from session
            var chairmanFacultyId = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(chairmanFacultyId))
            {
                return;
            }

            // Look up the user record to get their integer ID
            var user = _db.Users.FirstOrDefault(u => u.FacultyId == chairmanFacultyId);
            if (user == null)
            {
                return;
            }

            // Get all schedules for this faculty member using their integer ID
            var schedules = _db.Schedules
                .Where(s => s.FacultyId == user.Id)
                .AsNoTracking()
                .ToList();

            if (!schedules.Any())
            {
                return;
            }

            // --- Handling Nullable RoomId ---
            var roomIds = schedules
                .Where(s => s.RoomId.HasValue)
                .Select(s => s.RoomId.Value)
                .Distinct()
                .ToList();

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
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                    (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                    s.AssignedUnits.ToString(),
                    s.OfferCode
                ))
                .OrderBy(s => s.TimeSlot)
                .ToList();

            TTHSchedules = schedules
                .Where(s => !string.IsNullOrEmpty(s.TimeSlot) && (s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")))
                .Select(s => new ScheduleView(
                    s.TimeSlot,
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                    (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                    s.AssignedUnits.ToString(),
                    s.OfferCode
                ))
                .OrderBy(s => s.TimeSlot)
                .ToList();

            // Get all schedules and subjects assigned to this faculty member with semester info
            var allSchedulesWithSubjects = _db.Schedules
                .Where(s => s.FacultyId == user.Id)
                .Join(_db.Subjects, s => s.SubjectId, sub => sub.SubjectId, (s, sub) => new { Schedule = s, Subject = sub })
                .AsNoTracking()
                .ToList();

            // Extract all available years from the AcademicYear column in Schedule
            AvailableYears = allSchedulesWithSubjects
                .Select(ps => ps.Schedule.AcademicYear ?? "N/A")
                .Where(year => year != "N/A")
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Filter by selected year
            var filteredSubjects = allSchedulesWithSubjects;
            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                filteredSubjects = filteredSubjects
                    .Where(ps => (ps.Schedule.AcademicYear ?? "N/A") == SelectedYear)
                    .ToList();
            }

            // UPGRADED GROUPING: Group strictly by Academic Year (e.g., "Academic Year 2024-2025")
            SubjectHistory = filteredSubjects
                .GroupBy(ps => $"Academic Year {ps.Schedule.AcademicYear ?? "TBA"}")
                .Select(g => new SubjectGroup(
                    g.Key,
                    g.Select(ps => new SubjectDetail(
                        ps.Subject.Code,
                        ps.Subject.Title,
                        ps.Schedule.AssignedUnits.ToString(),
                        ps.Schedule.OfferCode
                    ))
                    .DistinctBy(s => $"{s.SubjectCode}-{s.SubjectTitle}")
                    .OrderBy(s => s.SubjectCode)
                    .ToList()
                ))
                .OrderByDescending(g => g.HeaderName) // Newest years at the top
                .ToList();

            // Calculate summary statistics
            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
            TotalUnits = MWFSchedules.Sum(s => int.Parse(s.Units ?? "0")) + TTHSchedules.Sum(s => int.Parse(s.Units ?? "0"));
            CurrentAcademicYear = schedules.FirstOrDefault()?.AcademicYear ?? "N/A";
        }
    }
}
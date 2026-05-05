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
        public record SubjectBySemester(string Semester, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();
        public List<SubjectBySemester> SubjectHistory { get; set; } = new();
        public List<string> AvailableYears { get; set; } = new();
        public List<string> AvailableSemesters { get; set; } = new();

        // Summary statistics
        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedSemester { get; set; }

        public void OnGet()
        {
            // Get the currently logged-in chairman's faculty ID from session
            var chairmanFacultyId = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(chairmanFacultyId))
            {
                // Not logged in, return empty
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

            // --- FIX FOR CS1503: Handling Nullable RoomId ---
            // Filter out nulls and extract the actual int values for the lookup
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
                    // Safely check if RoomId has a value before looking it up in the dictionary
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
                    // Safely check if RoomId has a value before looking it up in the dictionary
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
                .Where(year => year != "N/A")  // Filter out N/A values
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Extract all available semesters
            AvailableSemesters = allSchedulesWithSubjects
                .Select(ps => ps.Subject.Semester ?? "N/A")
                .Where(sem => sem != "N/A")  // Filter out N/A values
                .Distinct()
                .OrderByDescending(s => ExtractSemesterSortKey(s))
                .ToList();

            // Filter by selected year and semester if provided
            var filteredSubjects = allSchedulesWithSubjects;
            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                filteredSubjects = filteredSubjects
                    .Where(ps => (ps.Schedule.AcademicYear ?? "N/A") == SelectedYear)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(SelectedSemester) && SelectedSemester != "All")
            {
                filteredSubjects = filteredSubjects
                    .Where(ps => (ps.Subject.Semester ?? "N/A") == SelectedSemester)
                    .ToList();
            }

            // Group subjects by semester
            SubjectHistory = filteredSubjects
                .GroupBy(ps => ps.Subject.Semester ?? "N/A")
                .Select(g => new SubjectBySemester(
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
                .OrderByDescending(s => ExtractSemesterSortKey(s.Semester))
                .ToList();

            // Calculate summary statistics
            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
            TotalUnits = MWFSchedules.Sum(s => int.Parse(s.Units ?? "0")) + TTHSchedules.Sum(s => int.Parse(s.Units ?? "0"));
            CurrentAcademicYear = schedules.FirstOrDefault()?.AcademicYear ?? "N/A";
        }

        // Helper method to extract year from semester string
        // e.g., "4" from "4th Year - 2nd Sem"
        private static string ExtractYearFromSemester(string semester)
        {
            if (string.IsNullOrEmpty(semester)) return "N/A";

            try
            {
                var yearPart = semester.Split('-')[0].Trim().Split()[0]; // Get "4" from "4th Year"
                if (int.TryParse(yearPart, out var year))
                {
                    return $"{year}";
                }
            }
            catch { }

            return "N/A";
        }

        // Helper method to create a sortable key from semester string
        // e.g., "4th Year - 2nd Sem" -> "4-2" for proper sorting
        private static string ExtractSemesterSortKey(string semester)
        {
            if (string.IsNullOrEmpty(semester)) return "0-0";

            try
            {
                var parts = semester.Split('-');
                if (parts.Length < 2) return "0-0";

                var yearPart = parts[0].Trim().Split()[0]; // Get "4" from "4th Year"
                var semPart = parts[1].Trim().Split()[0];   // Get "2" from "2nd Sem"

                if (int.TryParse(yearPart, out var year) && int.TryParse(semPart, out var sem))
                {
                    // Sort descending: 4-2, 4-1, 3-2, 3-1, etc.
                    return $"{9 - year}-{3 - sem}";
                }
            }
            catch { }

            return "0-0";
        }
    }
}
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

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

        // MATCHING TEACHER PORTAL
        public record SubjectGroup(string HeaderName, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();

        public List<SubjectGroup> SubjectHistory { get; set; } = new();
        public List<string> AvailableYears { get; set; } = new();

        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedYear { get; set; }

        public void OnGet()
        {
            var chairmanFacultyIdString = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(chairmanFacultyIdString))
            {
                return;
            }

            int chairmanFacultyId = int.Parse(chairmanFacultyIdString);

            var user = _db.Users.FirstOrDefault(u => u.FacultyId == chairmanFacultyId);
            if (user == null)
            {
                return;
            }

            // DYNAMIC CHOICE GENERATION: Automatically build years based on calendar clocks
            int currentYear = DateTime.Today.Year;
            AvailableYears = new List<string>();
            for (int year = 2023; year <= currentYear + 1; year++)
            {
                AvailableYears.Add($"{year}-{year + 1}");
            }
            // Sort choices descending to show newest terms first
            AvailableYears = AvailableYears.OrderByDescending(y => y).ToList();

            // DEFAULT INJECTOR: Set filter default configuration state targeting current school year
            if (string.IsNullOrEmpty(SelectedYear))
            {
                SelectedYear = $"{currentYear}-{currentYear + 1}";
            }

            // Filter schedules for the Weekly Schedule view and totals based on the SelectedYear
            var schedulesQuery = _db.Schedules
                .Where(s => s.FacultyId == user.FacultyId);

            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                schedulesQuery = schedulesQuery.Where(s => s.AcademicYear == SelectedYear);
            }
            else if (SelectedYear == "All")
            {
                string defaultCurrentYear = $"{currentYear}-{currentYear + 1}";
                schedulesQuery = schedulesQuery.Where(s => s.AcademicYear == defaultCurrentYear);
            }

            var schedules = schedulesQuery.AsNoTracking().ToList();

            if (!schedules.Any())
            {
                return;
            }

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

            Func<string, bool> isMWF = timeSlot =>
            {
                if (string.IsNullOrWhiteSpace(timeSlot)) return false;
                int firstDigit = -1;
                for (int i = 0; i < timeSlot.Length; i++)
                {
                    if (char.IsDigit(timeSlot[i])) { firstDigit = i; break; }
                }
                string daysPart = firstDigit > 0 ? timeSlot.Substring(0, firstDigit) : timeSlot;
                var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                return tokens.Any(t => t == "M" || t == "W" || t == "F" || t == "S" ||
                                       t.Equals("MWF", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Mon", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Wed", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Fri", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Sat", StringComparison.OrdinalIgnoreCase));
            };

            Func<string, bool> isTTH = timeSlot =>
            {
                if (string.IsNullOrWhiteSpace(timeSlot)) return false;
                int firstDigit = -1;
                for (int i = 0; i < timeSlot.Length; i++)
                {
                    if (char.IsDigit(timeSlot[i])) { firstDigit = i; break; }
                }
                string daysPart = firstDigit > 0 ? timeSlot.Substring(0, firstDigit) : timeSlot;
                var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                return tokens.Any(t => t == "T" || t == "Th" ||
                                       t.Equals("TTH", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("TTh", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Tue", StringComparison.OrdinalIgnoreCase) ||
                                       t.Equals("Thu", StringComparison.OrdinalIgnoreCase));
            };

            MWFSchedules = schedules
                .Where(s => isMWF(s.TimeSlot))
                .Select(s => new ScheduleView(
                    s.TimeSlot,
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                    (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                    s.AssignedUnits.ToString(),
                    s.OfferCode
                ))
                .OrderBy(s => ParseStartTime(s.TimeSlot))
                .ToList();

            TTHSchedules = schedules
                .Where(s => isTTH(s.TimeSlot))
                .Select(s => new ScheduleView(
                    s.TimeSlot,
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                    subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                    (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                    s.AssignedUnits.ToString(),
                    s.OfferCode
                ))
                .OrderBy(s => ParseStartTime(s.TimeSlot))
                .ToList();

            var allSchedulesWithSubjects = _db.Schedules
                .Where(s => s.FacultyId == user.FacultyId)
                .Join(_db.Subjects, s => s.SubjectId, sub => sub.SubjectId, (s, sub) => new { Schedule = s, Subject = sub })
                .AsNoTracking()
                .ToList();

            var filteredSubjects = allSchedulesWithSubjects;
            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                filteredSubjects = filteredSubjects
                    .Where(ps => (ps.Schedule.AcademicYear ?? "N/A") == SelectedYear)
                    .ToList();
            }

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
                .OrderByDescending(g => g.HeaderName)
                .ToList();

            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
            TotalUnits = MWFSchedules.Sum(s => int.Parse(s.Units ?? "0")) + TTHSchedules.Sum(s => int.Parse(s.Units ?? "0"));
            CurrentAcademicYear = (SelectedYear == "All") ? $"{currentYear}-{currentYear + 1}" : SelectedYear;
        }

        private DateTime ParseStartTime(string timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return DateTime.MaxValue;
            try
            {
                var startPart = timeSlot.Split('-')[0].Trim();
                var timeString = new string(startPart.SkipWhile(c => !char.IsDigit(c)).ToArray());
                if (DateTime.TryParse(timeString, out DateTime parsedTime)) return parsedTime;
            }
            catch { }
            return DateTime.MaxValue;
        }
    }
}
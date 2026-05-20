using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System; // <-- Added this to handle Time formatting

namespace FTLSV2.Pages.Teacher
{
    public class TeacherPageModel : PageModel
    {
        private readonly FtlsDbContext _context;
        public TeacherPageModel(FtlsDbContext context) { _context = context; }

        public User LoggedInUser { get; set; }

        // --- Properties for Filtering ---
        public List<string> AvailableYears { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string SelectedYear { get; set; }
        public record ScheduleView(string TimeSlot, string SubjectCode, string SubjectTitle, string RoomName, string Units, int? OfferCode);
        public record SubjectGroup(string HeaderName, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();
        public List<SubjectGroup> SubjectHistory { get; set; } = new();
        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        public IActionResult OnGet()
        {
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // Fetch all schedules for this teacher
            var allSchedules = _context.Schedules
                .Where(s => s.FacultyId == LoggedInUser.FacultyId)
                .Include(s => s.Subject).Include(s => s.Room)
                .AsNoTracking().ToList();

            // Populate filter dropdown data
            AvailableYears = allSchedules.Select(s => s.AcademicYear).Where(y => y != null).Distinct().OrderByDescending(y => y).ToList();

            // 1. Map Current View (Separate by Day) AND SORT BY TIME
            MWFSchedules = allSchedules.Where(s => s.TimeSlot != null && s.TimeSlot.StartsWith("MWF"))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode))
                .OrderBy(s => ParseStartTime(s.TimeSlot)) // <-- Sorting applied here!
                .ToList();

            TTHSchedules = allSchedules.Where(s => s.TimeSlot != null && (s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode))
                .OrderBy(s => ParseStartTime(s.TimeSlot)) // <-- Sorting applied here!
                .ToList();

            // 2. Apply Filters for History Section
            var historyData = allSchedules;

            // Only filter by Year now
            if (!string.IsNullOrEmpty(SelectedYear))
            {
                historyData = historyData.Where(s => s.AcademicYear == SelectedYear).ToList();
            }

            // Group strictly by Academic Year (e.g., "Academic Year 2024-2025")
            SubjectHistory = historyData
                .GroupBy(s => $"Academic Year {s.AcademicYear ?? "TBA"}")
                .Select(g => new SubjectGroup(
                    g.Key,
                    g.Select(s => new SubjectDetail(s.Subject.Code, s.Subject.Title, s.AssignedUnits.ToString(), s.OfferCode))
                     .DistinctBy(s => s.SubjectCode)
                     .ToList()
                ))
                .OrderByDescending(g => g.HeaderName) // Newest years at the top
                .ToList();

            // 3. Populate Summary Stats
            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
            TotalUnits = allSchedules.Sum(s => s.AssignedUnits);
            CurrentAcademicYear = allSchedules.OrderByDescending(s => s.AcademicYear).FirstOrDefault()?.AcademicYear ?? "N/A";

            return Page();
        }

        // --- HELPER METHOD TO EXTRACT AND SORT REAL CLOCK TIME ---
        private DateTime ParseStartTime(string timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return DateTime.MaxValue;

            try
            {
                // Takes "MWF 8:00 AM - 9:00 AM" and splits it to get "MWF 8:00 AM"
                var startPart = timeSlot.Split('-')[0].Trim();

                // Extracts just the time part by skipping the "MWF " letters
                var timeString = new string(startPart.SkipWhile(c => !char.IsDigit(c)).ToArray());

                // Converts "8:00 AM" into a real C# Time object so 8AM mathematically comes before 1PM
                if (DateTime.TryParse(timeString, out DateTime parsedTime))
                {
                    return parsedTime;
                }
            }
            catch { }

            // Put unreadable/broken formats at the very bottom of the list
            return DateTime.MaxValue;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FTLSV2.Pages.Teacher
{
    public class TeacherPageModel : PageModel
    {
        private readonly FtlsDbContext _context;
        public TeacherPageModel(FtlsDbContext context) { _context = context; }

        public User LoggedInUser { get; set; }

        public List<string> AvailableYears { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string SelectedYear { get; set; }

        public record ScheduleView(string TimeSlot, string SubjectCode, string SubjectTitle, string RoomName, string Units, int? OfferCode, string DaysCsv);
        public record SubjectGroup(string HeaderName, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> Schedules { get; set; } = new();
        public List<SubjectGroup> SubjectHistory { get; set; } = new();
        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        public IActionResult OnGet()
        {
            var activeIdString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeIdString)) return RedirectToPage("/LoginPage");

            int activeId = int.Parse(activeIdString);

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // --- AUTOMATIC CHOICE GENERATION ---
            // Automatically scales from 2023 up to progressive relative values via system clock
            int currentYear = DateTime.Today.Year;
            AvailableYears = new List<string>();
            for (int year = 2023; year <= currentYear + 1; year++)
            {
                AvailableYears.Add($"{year}-{year + 1}");
            }
            AvailableYears = AvailableYears.OrderByDescending(y => y).ToList();

            // --- AUTOMATIC DEFAULT SELECTION ---
            // Forces filter landing calculation logic to lock directly to current active year parameters
            if (string.IsNullOrEmpty(SelectedYear))
            {
                SelectedYear = $"{currentYear}-{currentYear + 1}";
            }

            var allSchedules = _context.Schedules
                .Where(s => s.FacultyId == activeId)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .AsNoTracking()
                .ToList();

            // Map Current View dynamically parsing the days
            Schedules = allSchedules
                .Select(s => {
                    var days = new List<string>();
                    var timeSlot = s.TimeSlot ?? "";
                    
                    // Parse day tokens by splitting before the first digit (which starts the time range)
                    int firstDigit = timeSlot.IndexOfAny("0123456789".ToCharArray());
                    string daysPart = firstDigit != -1 ? timeSlot.Substring(0, firstDigit) : timeSlot;
                    
                    var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .ToList();

                    foreach (var t in tokens)
                    {
                        if (t == "M" || t.Equals("Mon", StringComparison.OrdinalIgnoreCase) || t.Equals("MWF", StringComparison.OrdinalIgnoreCase)) days.Add("Monday");
                        if (t == "T" || t.Equals("Tue", StringComparison.OrdinalIgnoreCase) || t.Equals("TTH", StringComparison.OrdinalIgnoreCase) || t.Equals("TTh", StringComparison.OrdinalIgnoreCase)) days.Add("Tuesday");
                        if (t == "W" || t.Equals("Wed", StringComparison.OrdinalIgnoreCase)) days.Add("Wednesday");
                        if (t == "Th" || t.Equals("Thu", StringComparison.OrdinalIgnoreCase)) days.Add("Thursday");
                        if (t == "F" || t.Equals("Fri", StringComparison.OrdinalIgnoreCase)) days.Add("Friday");
                        if (t == "S" || t.Equals("Sat", StringComparison.OrdinalIgnoreCase) || t.Equals("SAT", StringComparison.OrdinalIgnoreCase)) days.Add("Saturday");
                    }
                    
                    // Fallbacks for older formats
                    if (daysPart.Contains("MWF", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!days.Contains("Monday")) days.Add("Monday");
                        if (!days.Contains("Wednesday")) days.Add("Wednesday");
                        if (!days.Contains("Friday")) days.Add("Friday");
                    }
                    if (daysPart.Contains("TTH", StringComparison.OrdinalIgnoreCase) || daysPart.Contains("TTh", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!days.Contains("Tuesday")) days.Add("Tuesday");
                        if (!days.Contains("Thursday")) days.Add("Thursday");
                    }

                    return new ScheduleView(
                        timeSlot,
                        s.Subject.Code,
                        s.Subject.Title,
                        s.Room?.Name ?? "TBA",
                        s.AssignedUnits.ToString(),
                        s.OfferCode,
                        string.Join(",", days)
                    );
                })
                .OrderBy(s => ParseStartTime(s.TimeSlot ?? ""))
                .ToList();

            // Apply Filters
            var historyData = allSchedules;
            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                historyData = historyData.Where(s => s.AcademicYear == SelectedYear).ToList();
            }

            SubjectHistory = historyData
                .GroupBy(s => $"Academic Year {s.AcademicYear ?? "TBA"}")
                .Select(g => new SubjectGroup(
                    g.Key,
                    g.Select(s => new SubjectDetail(s.Subject.Code, s.Subject.Title, s.AssignedUnits.ToString(), s.OfferCode))
                     .DistinctBy(s => s.SubjectCode).ToList()
                ))
                .OrderByDescending(g => g.HeaderName).ToList();

            TotalClasses = Schedules.Count;
            TotalUnits = allSchedules.Sum(s => s.AssignedUnits);
            CurrentAcademicYear = $"{currentYear}-{currentYear + 1}";

            return Page();
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
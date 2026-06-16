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

            // Map Current View
            MWFSchedules = allSchedules.Where(s => s.TimeSlot != null && s.TimeSlot.StartsWith("MWF"))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode))
                .OrderBy(s => ParseStartTime(s.TimeSlot)).ToList();

            TTHSchedules = allSchedules.Where(s => s.TimeSlot != null && (s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode))
                .OrderBy(s => ParseStartTime(s.TimeSlot)).ToList();

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

            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
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
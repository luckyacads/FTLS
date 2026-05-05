using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages.Teacher
{
    public class TeacherPageModel : PageModel
    {
        private readonly FtlsDbContext _context;
        public TeacherPageModel(FtlsDbContext context) { _context = context; }

        public User LoggedInUser { get; set; }

        // --- Properties for Filtering ---
        public List<string> AvailableYears { get; set; } = new();
        public List<string> AvailableSemesters { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string SelectedYear { get; set; }
        [BindProperty(SupportsGet = true)] public string SelectedSemester { get; set; }

        public record ScheduleView(string TimeSlot, string SubjectCode, string SubjectTitle, string RoomName, string Units, int? OfferCode);
        public record SubjectBySemester(string Semester, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();
        public List<SubjectBySemester> SubjectHistory { get; set; } = new();
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
                .Where(s => s.FacultyId == LoggedInUser.Id)
                .Include(s => s.Subject).Include(s => s.Room)
                .AsNoTracking().ToList();

            // Populate filter dropdown data
            AvailableYears = allSchedules.Select(s => s.AcademicYear).Where(y => y != null).Distinct().OrderByDescending(y => y).ToList();
            AvailableSemesters = allSchedules.Select(s => s.Subject.Semester).Where(s => s != null).Distinct().ToList();

            // 1. Map Current View (Separate by Day)
            MWFSchedules = allSchedules.Where(s => s.TimeSlot != null && s.TimeSlot.StartsWith("MWF"))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode)).ToList();

            TTHSchedules = allSchedules.Where(s => s.TimeSlot != null && (s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")))
                .Select(s => new ScheduleView(s.TimeSlot, s.Subject.Code, s.Subject.Title, s.Room?.Name ?? "TBA", s.AssignedUnits.ToString(), s.OfferCode)).ToList();

            // 2. Apply Filters for History Section
            var historyData = allSchedules;
            if (!string.IsNullOrEmpty(SelectedYear)) historyData = historyData.Where(s => s.AcademicYear == SelectedYear).ToList();
            if (!string.IsNullOrEmpty(SelectedSemester)) historyData = historyData.Where(s => s.Subject.Semester == SelectedSemester).ToList();

            SubjectHistory = historyData.GroupBy(s => s.Subject.Semester ?? "N/A")
                .Select(g => new SubjectBySemester(g.Key, g.Select(s => new SubjectDetail(s.Subject.Code, s.Subject.Title, s.AssignedUnits.ToString(), s.OfferCode)).DistinctBy(s => s.SubjectCode).ToList()))
                .ToList();

            // 3. Populate Summary Stats
            TotalClasses = MWFSchedules.Count + TTHSchedules.Count;
            TotalUnits = allSchedules.Sum(s => s.AssignedUnits);
            CurrentAcademicYear = allSchedules.OrderByDescending(s => s.AcademicYear).FirstOrDefault()?.AcademicYear ?? "N/A";

            return Page();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public TeacherPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        // --- NEW VARIABLES FOR DYNAMIC STATS ---
        public int TotalUnits { get; set; }
        public string LoadStatus { get; set; }
        public string StatusColor { get; set; }

        // Holds their real schedule
        public IList<TeacherScheduleItem> MySchedules { get; set; }

        public IActionResult OnGet()
        {
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            // Find the specific logged-in user
            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // 1. Pull their actual schedule from the database!
            MySchedules = (from s in _context.Schedules
                           where s.FacultyId == LoggedInUser.Id // Using the teacher's ID
                           join sub in _context.Subjects on s.SubjectId equals sub.SubjectId
                           join r in _context.Rooms on s.RoomId equals r.RoomId
                           select new TeacherScheduleItem
                           {
                               CourseTitle = sub.Code + " - " + sub.Title,
                               ScheduleTime = s.TimeSlot,
                               RoomName = r.Name,
                               Units = sub.Units
                           }).ToList();

            // 2. Add up all the units
            TotalUnits = MySchedules.Sum(s => s.Units);

            // 3. SMART STATUS CHECK: Handling Overloads and Edge Cases!
            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            if (TotalUnits > LoggedInUser.MaxUnits || TotalUnits > globalMaxLimit)
            {
                // Edge Case 1: They have more units than their personal limit OR the global limit!
                LoadStatus = "OVERLOAD WARNING";
                StatusColor = "#e74c3c"; // Red
            }
            else if (TotalUnits == LoggedInUser.MaxUnits)
            {
                // Edge Case 2: They are exactly at their maximum allowed capacity
                LoadStatus = "MAX LIMIT REACHED";
                StatusColor = "#f39c12"; // Orange
            }
            else
            {
                // Normal Life: They are under the limit, so we hide the status!
                LoadStatus = "";
                StatusColor = "transparent";
            }

            return Page();
        }
    }

    // A helper class just to structure the data for the Teacher's table
    public class TeacherScheduleItem
    {
        public string CourseTitle { get; set; }
        public string ScheduleTime { get; set; }
        public string RoomName { get; set; }
        public int Units { get; set; }
    }
}
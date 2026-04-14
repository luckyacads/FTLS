using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages
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

            // 3. Determine their status based on the Admin Rules!
            int regularLimit = FTLSV2.Pages.AdminPageModel.CurrentRegularLoad;

            if (TotalUnits == 0)
            {
                LoadStatus = "No Load Assigned";
                StatusColor = "#7f8c8d"; // Grey
            }
            else if (TotalUnits <= regularLimit)
            {
                LoadStatus = "Regular Load";
                StatusColor = "#2ecc71"; // Green
            }
            else
            {
                LoadStatus = "Overload";
                StatusColor = "#f39c12"; // Orange
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
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class DashboardOverviewModel : PageModel
    {
        public record Assignment(string Faculty, string Course, string Schedule, string Room);

        public List<Assignment> Assignments { get; set; } = new();
        public int TotalFaculty { get; set; }
        public int TotalCourses { get; set; }

        public void OnGet()
        {
            // sample hard data for layout preview
            Assignments = new List<Assignment>
            {
                new Assignment("Engr. Harley", "B104 - Intro to Programming", "Mon/Wed 09:00 - 10:30", "Lab A"),
                new Assignment("Engr. Lucky", "B105 - Data Structures", "Tue/Thu 13:00 - 14:30", "Room 101")
            };

            TotalFaculty = 12; // example stat
            TotalCourses = 42; // example stat
        }
    }
}

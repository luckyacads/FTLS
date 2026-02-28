using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class ManageAssignmentsModel : PageModel
    {
        public record Assignment(string Faculty, string Course, string Time, string Room);

        public List<Assignment> CurrentAssignments { get; set; } = new();
        public List<string> FacultyOptions { get; set; } = new();
        public List<string> CourseOptions { get; set; } = new();

        public void OnGet()
        {
            // sample data
            CurrentAssignments = new List<Assignment>
            {
                new Assignment("Engr. Harley","B104", "Mon/Wed 09:00 - 10:30","Lab A"),
                new Assignment("Engr. Lucky","B105", "Tue/Thu 13:00 - 14:30","Room 101"),
            };

            FacultyOptions = new List<string>{ "Engr. Harley","Engr. Lucky","Engr. Charle" };
            CourseOptions = new List<string>{ "B104 - Intro to Programming","B105 - Data Structures","B106 - Software Development" };
        }

        public void OnPost()
        {
            // placeholder for post handling
        }
    }
}

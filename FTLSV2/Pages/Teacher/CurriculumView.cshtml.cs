using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages.Teacher
{
    public class CurriculumViewModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public CurriculumViewModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        // This will hold the unique subjects assigned to the teacher
        public IList<Subject> MySubjects { get; set; }

        // Filter lists
        public IList<Department> AllDepartments { get; set; }
        public IList<School> AllSchools { get; set; }

        public IActionResult OnGet()
        {
            // 1. Check who is logged in
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // 2. Look at their schedules, grab the subjects, and remove any duplicates!
            MySubjects = (from s in _context.Schedules
                          where s.FacultyId == LoggedInUser.Id // Find schedules for THIS teacher
                          join sub in _context.Subjects on s.SubjectId equals sub.SubjectId
                          select sub)
                          .Distinct() // Prevent duplicates if they teach the same subject twice
                          .ToList();

            // 3. Load all departments and schools for filter dropdowns
            AllDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
            AllSchools = _context.Schools.OrderBy(s => s.Name).ToList();

            return Page();
        }
    }
}
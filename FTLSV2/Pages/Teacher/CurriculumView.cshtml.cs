using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;
using FTLSV2.Models;

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

        // Container to use Schedule to fetch the new Semester column cleanly
        public List<Schedule> MySubjects { get; set; } = new List<Schedule>();
        public IList<Department> AllDepartments { get; set; }
        public IList<School> AllSchools { get; set; }

        public IActionResult OnGet()
        {
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // Query schedules, eager load the linked subjects + matching department profiles
            MySubjects = _context.Schedules
                .Include(s => s.Subject)
                    .ThenInclude(sub => sub.Department)
                .Where(s => s.FacultyId == LoggedInUser.Id)
                .ToList();

            // FIXED: Changed from _db to _context to match your constructor definition
            AllDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
            AllSchools = _context.Schools.OrderBy(s => s.Name).ToList();

            return Page();
        }
    }
}
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
    public class CurriculumCatalogModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public CurriculumCatalogModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        public List<Curriculum> MySubjects { get; set; } = new List<Curriculum>();

        public IList<Department> AllDepartments { get; set; }
        public IList<School> AllSchools { get; set; }

        public IActionResult OnGet()
        {
            var activeIdString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeIdString)) return RedirectToPage("/LoginPage");

            int activeId = int.Parse(activeIdString);

            LoggedInUser = _context.Users
                .Include(u => u.School)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.FacultyId == activeId);
            if (LoggedInUser == null) return RedirectToPage("/LoginPage");

            // 1. Fetch Curriculum & Departments from DB
            MySubjects = _context.Curriculums
                .Include(c => c.Department)
                .OrderBy(c => c.CurriculumYear)
                .ThenBy(c => c.YearLevel)
                .ThenBy(c => c.Semester)
                .ThenBy(c => c.CurriculumId)
                .ToList();

            // 2. Manually link Subjects in C#
            var allSubjects = _context.Subjects.ToList();
            foreach (var item in MySubjects)
            {
                item.Subject = allSubjects.FirstOrDefault(s => s.SubjectId == item.SubjectId);
            }

            AllDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
            AllSchools = _context.Schools.OrderBy(s => s.Name).ToList();

            return Page();
        }
    }
}

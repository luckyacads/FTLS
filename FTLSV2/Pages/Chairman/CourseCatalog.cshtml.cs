using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;

namespace FTLSV2.Pages.Chairman
{
    public class CourseCatalogModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public CourseCatalogModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record Course(string Code, string Title, int Units);

        public List<Course> CourseList { get; set; } = new();

        public void OnGet()
        {
            // Load courses from the subject table via the Subjects DbSet.
            // Order in the database first, then project to the lightweight Course record
            // so EF Core can translate the expression.
            CourseList = _db.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Code)
                .Select(s => new Course(s.Code, s.Title, s.Units))
                .ToList();
        }
    }
}

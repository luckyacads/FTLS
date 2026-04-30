using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;
using FTLSV2.Models;

namespace FTLSV2.Pages.Chairman
{
    public class FacultyProfilesModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public FacultyProfilesModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record FacultyView(string Name, string Email, int CurrentLoad);

        public List<FacultyView> FacultyList { get; set; } = new();

        public void OnGet()
        {
            // Get ALL active teachers directly from the Users table
            FacultyList = _db.Users
                .Where(u => u.Role == "Teacher" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                .Select(t => new FacultyView(
                    $"Engr. {t.FirstName} {t.LastName}",
                    t.Email,
                    0  // Default load to 0 from Users table
                ))
                .ToList();
        }
    }
}
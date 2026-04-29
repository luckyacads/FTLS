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
            // 1. Get ALL active teachers directly from the Users table so nobody is skipped!
            var allTeachers = _db.Users
                .Where(u => u.Role == "Teacher" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                .ToList();

            // 2. Get the calculated loads from your existing summary view
            var summaries = _db.FacultyLoadSummaries.ToList();

            // 3. Match them up! If they aren't in the summary view, they get 0 units.
            FacultyList = allTeachers.Select(t =>
            {
                // Look for a matching email in the summary view
                var summary = summaries.FirstOrDefault(s => s.Email == t.Email);

                // If summary exists, use its CurrentLoad. Otherwise, default to 0.
                int load = summary != null ? summary.CurrentLoad : 0;

                return new FacultyView(
                    $"Engr. {t.FirstName} {t.LastName}",
                    t.Email,
                    load
                );
            }).ToList();
        }
    }
}
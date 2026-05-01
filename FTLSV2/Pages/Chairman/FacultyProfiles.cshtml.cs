using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;

namespace FTLSV2.Pages.Chairman
{
    public class FacultyProfilesModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public FacultyProfilesModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record FacultyView(
            int Id,
            string FacultyId,
            string Name,
            string Role,
            string CurrentLoadDisplay
        );

        public List<FacultyView> FacultyList { get; set; } = new();

        public void OnGet()
        {
            // Step 1: fully load users first
            var allowedRoles = new[] { "Teacher", "Faculty", "Chairman" };

            var teachers = _db.Users
                .Where(u =>
                    allowedRoles.Contains(u.Role) &&
                    (u.Status == "Active" || string.IsNullOrEmpty(u.Status))
                )
                .ToList();

            // Step 2: now safe to query schedules
            FacultyList = teachers
                .Select(u =>
                {
                    var total = _db.Schedules
                        .Where(s => s.FacultyId == u.Id)
                        .Sum(s => (int?)s.AssignedUnits);

                    return new FacultyView(
                        u.Id,
                        u.FacultyId,
                        $"{u.FirstName} {u.LastName}",
                        u.Role,
                        total == null ? "Pending" : $"{total} units"
                    );
                })
                .OrderBy(f => f.Name)
                .ToList();
        }
    }
}
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;
using Microsoft.AspNetCore.Http;

namespace FTLSV2.Pages.Chairman
{
    public class FacultyProfilesModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public FacultyProfilesModel(FtlsDbContext db)
        {
            _db = db;
        }

        // FIXED: FacultyId is now an int. Removed unused 'Id' property.
        public record FacultyView(
            int FacultyId,
            string Name,
            string Role,
            string CurrentUnitsDisplay
        );

        public List<FacultyView> FacultyList { get; set; } = new();

        public void OnGet()
        {
            var activeFacultyIdString = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeFacultyIdString))
            {
                FacultyList = new List<FacultyView>();
                return;
            }

            // FIXED: Parse the session string to an integer
            int activeFacultyId = int.Parse(activeFacultyIdString);

            // Get chairman viewing the page
            var chairman = _db.Users.FirstOrDefault(u =>
                u.FacultyId == activeFacultyId &&
                u.Role == "Chairman");

            if (chairman == null || chairman.DepartmentId == null)
            {
                FacultyList = new List<FacultyView>();
                return;
            }

            int departmentId = chairman.DepartmentId.Value;

            var allowedRoles = new[] { "Faculty", "Chairman" };

            // Only same department
            var departmentUsers = _db.Users
                .Where(u =>
                    allowedRoles.Contains(u.Role) &&
                    u.DepartmentId == departmentId &&
                    (u.Status == "Active" || string.IsNullOrEmpty(u.Status))
                )
                .ToList();

            FacultyList = departmentUsers
                .Select(u =>
                {
                    var totalUnits = _db.Schedules
                        .Where(s => s.FacultyId == u.FacultyId)
                        .Sum(s => (int?)s.AssignedUnits);

                    return new FacultyView(
                        u.FacultyId,
                        $"{u.FirstName} {u.LastName}",
                        u.Role,
                        totalUnits == null ? "Pending" : $"{totalUnits} units"
                    );
                })
                .OrderBy(f => f.Name)
                .ToList();
        }
    }
}
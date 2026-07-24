using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;
using Microsoft.AspNetCore.Http;
using System;

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
            int FacultyId,
            string Name,
            string Role,
            int TotalUnits,
            int MaxUnits,
            bool IsUnderloaded,
            bool IsOverloaded,
            string CurrentUnitsDisplay
        );

        public List<FacultyView> FacultyList { get; set; } = new();

        public string SelectedAcademicYear { get; set; } = string.Empty;

        public string CurrentTermDisplay { get; set; } = string.Empty;

        public void OnGet()
        {
            var activeFacultyIdString = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeFacultyIdString))
            {
                FacultyList = new List<FacultyView>();
                return;
            }

            int activeFacultyId = int.Parse(activeFacultyIdString);

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

            var departmentUsers = _db.Users
                .Where(u =>
                    allowedRoles.Contains(u.Role) &&
                    u.DepartmentId == departmentId &&
                    !u.Is_delete &&
                    (u.Status == "Active" || string.IsNullOrEmpty(u.Status))
                )
                .ToList();

            // Calculate Academic Year dynamically
            int currentYear = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            if (month >= 1 && month <= 5)
            {
                SelectedAcademicYear = $"{currentYear - 1}-{currentYear}";
            }
            else
            {
                SelectedAcademicYear = $"{currentYear}-{currentYear + 1}";
            }

            CurrentTermDisplay = $"A.Y. {SelectedAcademicYear}";

            // Auto-sync policy updates to database records
            bool changesMade = false;
            foreach (var user in departmentUsers)
            {
                bool isFT = user.MaxUnits >= 18;
                int targetMax = isFT ? 30 : 17;
                if (user.MaxUnits != targetMax)
                {
                    user.MaxUnits = targetMax;
                    changesMade = true;
                }
            }
            if (changesMade)
            {
                _db.SaveChanges();
            }

            FacultyList = departmentUsers
                .Select(u =>
                {
                    int totalUnits = _db.Schedules
                        .Where(s => s.FacultyId == u.FacultyId
                                    && s.AcademicYear == SelectedAcademicYear)
                        .Sum(s => s.AssignedUnits);

                    int maxUnits = u.MaxUnits;
                    bool isFullTime = maxUnits >= 18;

                    bool isUnderloaded = isFullTime && totalUnits < 18;
                    bool isOverloaded = totalUnits > maxUnits;

                    string displayUnits = isUnderloaded
                        ? $"{totalUnits} units (Below 18 Units)"
                        : $"{totalUnits} / {maxUnits} units";

                    return new FacultyView(
                        u.FacultyId,
                        $"{u.FirstName} {u.LastName}",
                        u.Role,
                        totalUnits,
                        maxUnits,
                        isUnderloaded,
                        isOverloaded,
                        displayUnits
                    );
                })
                .OrderBy(f => f.Name)
                .ToList();
        }
    }
}
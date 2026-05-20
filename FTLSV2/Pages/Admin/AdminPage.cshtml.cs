using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;

namespace FTLSV2.Pages.Admin
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public AdminPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }
        public IList<User> DbUsers { get; set; } = new List<User>();

        // Dictionary to tell the frontend if a user has schedules
        public Dictionary<string, bool> UserHasSchedules { get; set; } = new();

        [BindProperty] public int InputMaxOverload { get; set; }

        public IActionResult OnGet()
        {
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            var activeId = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeId))
            {
                return RedirectToPage("/LoginPage");
            }

            DbUsers = _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            LoggedInUser = _context.Users
                .FirstOrDefault(u => u.FacultyId == activeId);

            // schedule.faculty_id now references users.faculty_id, not users.id.
            var facultyIds = DbUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.FacultyId))
                .Select(u => u.FacultyId)
                .ToList();

            var facultiesWithSchedules = _context.Schedules
                .Where(s => facultyIds.Contains(s.FacultyId))
                .Select(s => s.FacultyId)
                .Distinct()
                .ToHashSet();

            foreach (var user in DbUsers)
            {
                UserHasSchedules[user.FacultyId] =
                    !string.IsNullOrWhiteSpace(user.FacultyId) &&
                    facultiesWithSchedules.Contains(user.FacultyId);
            }

            var settings = _context.SystemSettings.FirstOrDefault();

            if (settings == null)
            {
                settings = new SystemSettings { MaxOverload = 21 };
                _context.SystemSettings.Add(settings);
                _context.SaveChanges();
            }

            InputMaxOverload = settings.MaxOverload;

            return Page();
        }

        // --- HANDLES ACTIVATING / DEACTIVATING USERS ---
        public IActionResult OnPostToggleUserStatus(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (dbUser != null)
            {
                try
                {
                    if (dbUser.Status == "Active" || string.IsNullOrEmpty(dbUser.Status))
                    {
                        dbUser.Status = "Inactive";
                    }
                    else
                    {
                        dbUser.Status = "Active";
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"{dbUser.FirstName}'s account is now {dbUser.Status}!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating status. {ex.Message}";
                }
            }

            return RedirectToPage();
        }

        // --- ASSIGN / UPDATE USER ROLE ---
        public IActionResult OnPostUpdateUserRole(string facultyId, string newRole)
        {
            var allowedRoles = new[] { "Faculty", "Chairman" };

            if (string.IsNullOrWhiteSpace(newRole) || !allowedRoles.Contains(newRole))
            {
                TempData["ErrorMessage"] = "Invalid role selected. Only Faculty and Chairman roles are allowed.";
                return RedirectToPage();
            }

            var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User account could not be found.";
                return RedirectToPage();
            }

            try
            {
                if (user.Role == "Admin")
                {
                    TempData["ErrorMessage"] = "Administrator accounts cannot be modified from this role assignment option.";
                    return RedirectToPage();
                }

                // Restriction: only one Chairman per department.
                if (newRole == "Chairman")
                {
                    if (!user.DepartmentId.HasValue)
                    {
                        TempData["ErrorMessage"] = "This user cannot be assigned as Chairman because no department is assigned to this account.";
                        return RedirectToPage();
                    }

                    var existingChairman = _context.Users
                        .FirstOrDefault(u =>
                            u.FacultyId != user.FacultyId &&
                            u.DepartmentId == user.DepartmentId &&
                            u.Role == "Chairman");

                    if (existingChairman != null)
                    {
                        var department = _context.Departments
                            .FirstOrDefault(d => d.Id == user.DepartmentId.Value);

                        string departmentName = department != null
                            ? $"{department.Name} ({department.Code})"
                            : "this department";

                        TempData["ErrorMessage"] =
                            $"{departmentName} already has an assigned Chairman: {existingChairman.FirstName} {existingChairman.LastName}. " +
                            "Only one Chairman is allowed per department. Change the existing Chairman back to Faculty first before assigning another one.";

                        return RedirectToPage();
                    }
                }

                user.Role = newRole;
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Role for {user.FirstName} {user.LastName} updated to {newRole}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating role. {ex.Message}";
            }

            return RedirectToPage();
        }

        // --- UPDATE USER MAX UNITS ---
        public IActionResult OnPostUpdateUserUnits(string facultyId, int newUnits)
        {
            var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (user != null)
            {
                var settings = _context.SystemSettings.FirstOrDefault();
                int max = settings?.MaxOverload ?? 21;

                if (newUnits > max)
                {
                    TempData["ErrorMessage"] = $"Cannot exceed {max} units!";
                    return RedirectToPage();
                }

                user.MaxUnits = newUnits;
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Updated units for {user.FirstName}.";
            }

            return RedirectToPage();
        }

        // --- RESET USER PASSWORD ---
        public IActionResult OnPostResetPassword(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (dbUser != null)
            {
                dbUser.Password = "usjr1234";
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Password for {dbUser.FirstName} has been reset to 'usjr1234'.";
            }

            return RedirectToPage();
        }

        // --- DELETE USER ---
        public IActionResult OnPostDeleteUser(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (dbUser != null)
            {
                // schedule.faculty_id now stores the 5-digit faculty ID.
                var userSchedules = _context.Schedules
                    .Where(s => s.FacultyId == dbUser.FacultyId)
                    .ToList();

                if (userSchedules.Any())
                {
                    _context.Schedules.RemoveRange(userSchedules);
                }

                _context.Users.Remove(dbUser);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Account for {dbUser.FirstName} {dbUser.LastName} has been permanently deleted.";
            }

            return RedirectToPage();
        }
    }
}
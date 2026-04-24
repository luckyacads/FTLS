using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Claims;

namespace FTLSV2.Pages.Admin
{
    public class AdminPageModel : PageModel
    {
        // 1. Database Connection
        private readonly FtlsDbContext _context;

        public AdminPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        // 2. List to hold real users from the database
        public IList<User> DbUsers { get; set; }

        // --- BIND PROPERTIES FOR NEW USER FORM ---
        [BindProperty]
        public string InputFirstName { get; set; }

        [BindProperty]
        public string InputLastName { get; set; }

        [BindProperty]
        public string InputFacultyId { get; set; }

        [BindProperty]
        public string InputEmail { get; set; }

        [BindProperty]
        public string InputPassword { get; set; }

        [BindProperty]
        public string InputRole { get; set; }

        [BindProperty]
        public int InputMaxUnits { get; set; }

        // --- LOAD CONSTRAINT DATA (Cleaned up!) ---

        [BindProperty]
        public int InputMaxOverload { get; set; }

        // --- GET METHOD ---
        public void OnGet()
        {
            DbUsers = _context.Users.ToList();
            var activeId = HttpContext.Session.GetString("UserId");

            LoggedInUser = _context.Users
                .FirstOrDefault(u => u.FacultyId == activeId);

            var settings = _context.SystemSettings.FirstOrDefault();

            if (settings == null)
            {
                settings = new SystemSettings { MaxOverload = 21 };
                _context.SystemSettings.Add(settings);
                _context.SaveChanges();
            }

            InputMaxOverload = settings.MaxOverload;
        }

        // --- POST METHODS ---
        public IActionResult OnPostAddUser()
        {
            var newUser = new User
            {
                FacultyId = InputFacultyId,
                FirstName = InputFirstName,
                LastName = InputLastName,
                Email = InputEmail,
                Password = InputPassword,
                Role = InputRole,
                Status = "Active",
                MaxUnits = InputMaxUnits,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(newUser);
                AddAuditEntryToContext($"Created user: {InputFirstName} {InputLastName}");
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Account for {InputFirstName} {InputLastName} created successfully!";
            }
            catch (Exception ex)
            {
                var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error adding user: {baseMsg}");
                TempData["ErrorMessage"] = $"An error occurred while creating the user. {baseMsg}";
            }
            return RedirectToPage();
        }

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

                    AddAuditEntryToContext($"Changed status for {dbUser.FirstName} {dbUser.LastName} to {dbUser.Status}");
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"{dbUser.FirstName}'s account is now {dbUser.Status}!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error toggling user status: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while updating the user status. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostResetPassword(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
            {
                try
                {
                    dbUser.Password = "usjr1234";
                    AddAuditEntryToContext($"Reset password for {dbUser.FirstName} {dbUser.LastName}");
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Password for {dbUser.FirstName} has been reset to 'usjr1234'.";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error resetting password: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while resetting the password. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteUser(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
            {
                try
                {
                    _context.Users.Remove(dbUser);
                    AddAuditEntryToContext($"Deleted user: {dbUser.FirstName} {dbUser.LastName}");
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Account for {dbUser.FirstName} {dbUser.LastName} has been permanently deleted.";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error deleting user: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while deleting the user. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateRules()
        {
            var settings = _context.SystemSettings.FirstOrDefault();

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    MaxOverload = InputMaxOverload
                };
                _context.SystemSettings.Add(settings);
            }
            else
            {
                settings.MaxOverload = InputMaxOverload;
            }

            try
            {
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Global Max Overload updated to {InputMaxOverload}!";
            }
            catch (Exception ex)
            {
                var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error updating rules: {baseMsg}");
                TempData["ErrorMessage"] = $"An error occurred while updating rules. {baseMsg}";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateUserUnits(string facultyId, int newUnits)
        {
            var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);

            if (user != null)
            {
                // 🔒 Get global max limit
                var settings = _context.SystemSettings.FirstOrDefault();
                int max = settings?.MaxOverload ?? 21;

                // 🚫 Prevent exceeding limit
                if (newUnits > max)
                {
                    TempData["ErrorMessage"] = $"Cannot exceed {max} units!";
                    return RedirectToPage();
                }

                try
                {
                    user.MaxUnits = newUnits;
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Updated units for {user.FirstName}.";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error updating user units: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while updating user units. {baseMsg}";
                }
            }

            return RedirectToPage(); // 🔥 IMPORTANT
        }

        private void AddAuditEntryToContext(string action)
        {
            // Get the currently logged-in user's FacultyId from session
            var facultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? HttpContext.Session.GetString("ActiveUser");
            int? userId = null;

            if (!string.IsNullOrEmpty(facultyId))
            {
                // Look up the user ID from the database using FacultyId
                var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var auditLog = new AuditLog
            {
                // Use UTC to match PostgreSQL 'timestamp with time zone' requirements
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Action = action
            };

            // Add to context but do NOT call SaveChanges here. Caller will persist.
            _context.AuditLogs.Add(auditLog);
        }
    }
}
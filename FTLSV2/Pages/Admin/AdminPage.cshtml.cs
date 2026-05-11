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
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public AdminPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }
        public IList<User> DbUsers { get; set; }

        [BindProperty] public string InputFirstName { get; set; }
        [BindProperty] public string InputLastName { get; set; }
        [BindProperty] public string InputFacultyId { get; set; }
        [BindProperty] public string InputEmail { get; set; }
        [BindProperty] public string InputPassword { get; set; }
        [BindProperty] public string InputRole { get; set; }
        [BindProperty] public int InputMaxUnits { get; set; }
        [BindProperty] public int InputMaxOverload { get; set; }

        public IActionResult OnGet()
        {
            // --- DIRECT HTTP CACHE KILLER ---
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            var activeId = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeId))
            {
                return RedirectToPage("/LoginPage");
            }

            DbUsers = _context.Users.OrderByDescending(u => u.CreatedAt).ToList();

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);

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
                TempData["ErrorMessage"] = $"An error occurred while creating the user. {baseMsg}";
            }
            return RedirectToPage();
        }

        // --- HANDLES ACTIVATING NEW REGISTRATIONS ---
        public IActionResult OnPostToggleUserStatus(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
            {
                try
                {
                    // If they are Active, make them Inactive. Otherwise (Pending/Inactive), activate them!
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
                    TempData["ErrorMessage"] = $"Error updating status. {ex.Message}";
                }
            }
            return RedirectToPage();
        }

        // --- NEW: ASSIGN ROLE TO NEW USERS ---
        public IActionResult OnPostUpdateUserRole(string facultyId, string newRole)
        {
            var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (user != null)
            {
                try
                {
                    user.Role = newRole;
                    AddAuditEntryToContext($"Assigned role '{newRole}' to {user.FirstName} {user.LastName}");
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Role for {user.FirstName} updated to {newRole}.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating role. {ex.Message}";
                }
            }
            return RedirectToPage();
        }

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

        public IActionResult OnPostResetPassword(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
            {
                dbUser.Password = "usjr1234";
                AddAuditEntryToContext($"Reset password for {dbUser.FirstName} {dbUser.LastName}");
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Password for {dbUser.FirstName} has been reset to 'usjr1234'.";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteUser(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
            {
                _context.Users.Remove(dbUser);
                AddAuditEntryToContext($"Deleted user: {dbUser.FirstName} {dbUser.LastName}");
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Account for {dbUser.FirstName} {dbUser.LastName} has been permanently deleted.";
            }
            return RedirectToPage();
        }

        private void AddAuditEntryToContext(string action)
        {
            var facultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? HttpContext.Session.GetString("ActiveUser");
            int? userId = null;

            if (!string.IsNullOrEmpty(facultyId))
            {
                var user = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
                if (user != null) userId = user.Id;
            }

            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Action = action
            };
            _context.AuditLogs.Add(auditLog);
        }
    }
}
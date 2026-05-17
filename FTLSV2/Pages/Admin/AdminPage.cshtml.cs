using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Claims;
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
        public IList<User> DbUsers { get; set; }

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

            if (user != null)
            {
                try
                {
                    if (user.Role == "Admin")
                    {
                        TempData["ErrorMessage"] = "Administrator accounts cannot be modified from this role assignment option.";
                        return RedirectToPage();
                    }

                    user.Role = newRole;
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
                _context.Users.Remove(dbUser);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Account for {dbUser.FirstName} {dbUser.LastName} has been permanently deleted.";
            }

            return RedirectToPage();
        }
    }
}
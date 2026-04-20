using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FTLSV2.Pages
{
    public class AdminPageModel : PageModel
    {
        // 1. Database Connection
        private readonly FtlsDbContext _context;

        public AdminPageModel(FtlsDbContext context)
        {
            _context = context;
        }

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

            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Account for {InputFirstName} {InputLastName} created successfully!";
            return RedirectToPage();
        }

        public IActionResult OnPostToggleUserStatus(string facultyId)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.FacultyId == facultyId);
            if (dbUser != null)
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
            return RedirectToPage();
        }

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

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Global Max Overload updated to {InputMaxOverload}!";
            return RedirectToPage();
        }
    }
}
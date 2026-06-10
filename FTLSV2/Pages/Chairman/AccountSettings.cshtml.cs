using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FTLSV2.Pages.Chairman
{
    public class AccountSettingsModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public AccountSettingsModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        [BindProperty]
        public string CurrentPassword { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        // NEW: Binding property parameter tracking confirm input state
        [BindProperty]
        public string ConfirmNewPassword { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            var activeIdString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeIdString)) return RedirectToPage("/LoginPage");

            int activeId = int.Parse(activeIdString);
            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);

            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            var activeIdString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeIdString)) return RedirectToPage("/LoginPage");

            int activeId = int.Parse(activeIdString);
            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);

            // 1. Verify old password matches record
            if (LoggedInUser.Password != CurrentPassword)
            {
                ErrorMessage = "Your current password is incorrect.";
                return Page();
            }

            // 2. NEW: Validation rule checking character boundary conditions
            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8)
            {
                ErrorMessage = "The new password must be at least 8 characters long.";
                return Page();
            }

            // 3. NEW: Security logic matching verification parity state
            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "The new password confirmation does not match.";
                return Page();
            }

            // 4. Update and commit to your DB database layer
            LoggedInUser.Password = NewPassword;
            _context.SaveChanges();

            SuccessMessage = "Password successfully updated!";
            return Page();
        }
    }
}
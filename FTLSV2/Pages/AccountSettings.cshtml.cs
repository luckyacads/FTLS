using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FTLSV2.Pages
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

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);
            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            var activeId = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeId)) return RedirectToPage("/LoginPage");

            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);

            // Check if they typed their old password correctly
            if (LoggedInUser.Password != CurrentPassword)
            {
                ErrorMessage = "Your current password is incorrect.";
                return Page();
            }

            // Save the new password to NeonDB
            LoggedInUser.Password = NewPassword;
            _context.SaveChanges();

            SuccessMessage = "Password successfully updated!";
            return Page();
        }
    }
}
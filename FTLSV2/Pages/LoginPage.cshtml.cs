using FTLSV2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Pages
{
    public class LoginPageModel : PageModel
    {
        // --- ADD THESE LINES HERE ---
        private readonly FtlsDbContext _context;

        public LoginPageModel(FtlsDbContext context)
        {
            _context = context;
        }
        // ----------------------------

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            Username = Username?.Trim() ?? string.Empty;
            Password = Password?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(Username) || Username.Length != 5 || !Username.All(char.IsDigit))
            {
                ErrorMessage = "Faculty ID must be exactly 5 digits.";
                return Page();
            }

            var dbUser = _context.Users.FirstOrDefault(u =>
                u.FacultyId == Username &&
                u.Password == Password);

            if (dbUser != null)
            {
                if (dbUser.Status == "Inactive")
                {
                    ErrorMessage = "This account has been deactivated. Please contact the Administrator.";
                    return Page();
                }

                HttpContext.Session.SetString("ActiveUser", dbUser.FacultyId);
                HttpContext.Session.SetString("ActiveUserName", dbUser.FirstName + " " + dbUser.LastName);
                HttpContext.Session.SetString("ActiveUserEmail", dbUser.Email);
                HttpContext.Session.SetString("UserRole", dbUser.Role);

                if (dbUser.Role == "Admin")
                {
                    return RedirectToPage("/Admin/AdminPage");
                }
                else if (dbUser.Role == "Chairman")
                {
                    return RedirectToPage("/Chairman/ChairmanPage");
                }
                else if (dbUser.Role == "Faculty")
                {
                    return RedirectToPage("/Teacher/TeacherPage");
                }
            }

            ErrorMessage = "Invalid Faculty ID or Password.";
            return Page();
        }

        // This runs when someone clicks the Logout button
        public IActionResult OnGetLogout()
        {
            // 1. Wipe the memory!
            HttpContext.Session.Clear();

            // 2. Send them back to a fresh login screen
            return RedirectToPage("/LoginPage");
        }
    }
}
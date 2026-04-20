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
            if (string.IsNullOrEmpty(Username) || Username.Length < 8)
            {
                ErrorMessage = "Faculty ID must be at least 8 characters long.";
                return Page();
            }

            // 1. Ask NeonDB: "Is there a user with this exact ID and Password?"
            var dbUser = _context.Users.FirstOrDefault(u =>
                u.FacultyId == Username &&
                u.Password == Password);

            // 2. If we found a match, check their status!
            if (dbUser != null)
            {
                // --- THIS IS THE NEW SECURITY BLOCK ---
                if (dbUser.Status == "Inactive")
                {
                    ErrorMessage = "This account has been deactivated. Please contact the Administrator.";
                    return Page();
                }
                // --------------------------------------
                HttpContext.Session.SetString("ActiveUser", dbUser.FacultyId);
                HttpContext.Session.SetString("ActiveUserName", dbUser.FirstName + " " + dbUser.LastName);
                HttpContext.Session.SetString("ActiveUserEmail", dbUser.Email);

                // 3. If they are Active, route them normally
                if (dbUser.Role == "Admin")
                {
                    return RedirectToPage("/AdminPage");
                }
                else if (dbUser.Role == "Chairman")
                {
                    return RedirectToPage("/Chairman/ChairmanPage");
                }
                else if (dbUser.Role == "Teacher" || dbUser.Role == "Faculty")
                {
                    return RedirectToPage("/TeacherPage");
                }
            }
            else
            {
                // If dbUser is null, they typed the wrong ID or Password
                ErrorMessage = "Invalid Faculty ID or Password.";
                return Page();
            }

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
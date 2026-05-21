using FTLSV2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FTLSV2.Pages
{
    public class LoginPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public LoginPageModel(FtlsDbContext context)
        {
            _context = context;
        }

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

            // Security check: Make sure it's exactly 5 numbers
            if (string.IsNullOrEmpty(Username) || Username.Length != 5 || !Username.All(char.IsDigit))
            {
                ErrorMessage = "Faculty ID must be exactly 5 digits.";
                return Page();
            }

            // Convert the string username to an integer to match the new database schema
            int activeUserId = int.Parse(Username);

            // Query using the integer ID
            var dbUser = _context.Users.FirstOrDefault(u =>
                u.FacultyId == activeUserId &&
                u.Password == Password);

            if (dbUser != null)
            {
                if (dbUser.Status == "Inactive")
                {
                    ErrorMessage = "This account has been deactivated. Please contact the Administrator.";
                    return Page();
                }

                // Convert FacultyId back to a string just for the Session memory
                HttpContext.Session.SetString("ActiveUser", dbUser.FacultyId.ToString());
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

        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/LoginPage");
        }
    }
}
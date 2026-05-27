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

            if (string.IsNullOrEmpty(Username) || Username.Length != 5 || !Username.All(char.IsDigit))
            {
                ErrorMessage = "Faculty ID must be exactly 5 digits.";
                return Page();
            }

            int activeUserId = int.Parse(Username);

            var dbUser = _context.Users
                .Where(u => u.FacultyId == activeUserId && u.Password == Password)
                .Select(u => new
                {
                    FacultyId = u.FacultyId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role,
                    Status = u.Status
                })
                .FirstOrDefault();

            if (dbUser == null)
            {
                ErrorMessage = "Invalid Faculty ID or Password.";
                return Page();
            }

            if (dbUser.Status == "Inactive")
            {
                ErrorMessage = "This account has been deactivated. Please contact the Administrator.";
                return Page();
            }

            HttpContext.Session.SetString("ActiveUser", dbUser.FacultyId.ToString());
            HttpContext.Session.SetString("ActiveUserName", $"{dbUser.FirstName} {dbUser.LastName}");
            HttpContext.Session.SetString("ActiveUserEmail", dbUser.Email ?? string.Empty);
            HttpContext.Session.SetString("UserRole", dbUser.Role ?? string.Empty);

            if (dbUser.Role == "Admin")
            {
                return RedirectToPage("/Admin/AdminPage");
            }

            if (dbUser.Role == "Chairman")
            {
                return RedirectToPage("/Chairman/ChairmanPage");
            }

            if (dbUser.Role == "Faculty")
            {
                return RedirectToPage("/Teacher/TeacherPage");
            }

            ErrorMessage = "Invalid user role. Please contact the Administrator.";
            return Page();
        }

        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/LoginPage");
        }
    }
}
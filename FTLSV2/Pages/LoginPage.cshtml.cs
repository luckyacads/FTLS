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

            // 2. If we found a match, check their role and send them to the right page!
            if (dbUser != null)
            {
                if (dbUser.Role == "Admin")
                {
                    return RedirectToPage("/AdminPage");
                }
                else if (dbUser.Role == "Teacher")
                {
                    return RedirectToPage("/TeacherPage");
                }
                else if (dbUser.Role == "Chairman")
                {
                    return RedirectToPage("/Chairman/ChairmanPage");
                }
                else
                {
                    // Fallback just in case they don't have a role assigned
                    return RedirectToPage("/Index");
                }
            }
            else
            {
                // If NeonDB returns null, they typed the wrong ID or password
                ErrorMessage = "Invalid Faculty ID or Password.";
                return Page();
            }
        }
    }
}
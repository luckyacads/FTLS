using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FTLSV2.Pages
{
    public class TeacherPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public TeacherPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        public User LoggedInUser { get; set; }

        public IActionResult OnGet()
        {
            // 1. Check the backpack to see who is logged in
            var activeId = HttpContext.Session.GetString("ActiveUser");

            // 2. If nobody is logged in, kick them back to the login screen
            if (string.IsNullOrEmpty(activeId))
            {
                return RedirectToPage("/LoginPage");
            }

            // 3. Pull their real info from NeonDB!
            LoggedInUser = _context.Users.FirstOrDefault(u => u.FacultyId == activeId);

            return Page();
        }
    }
}
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FTLSV2.Pages.Chairman
{
    public class RegisterTeacherModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public RegisterTeacherModel(FtlsDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string FacultyId { get; set; } = "";

        [BindProperty]
        public string FirstName { get; set; } = "";

        [BindProperty]
        public string LastName { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string Message { get; set; } = "";

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Chairman")
                return RedirectToPage("/LoginPage");

            return Page();
        }

        public IActionResult OnPost()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var facultyIdSession = HttpContext.Session.GetString("ActiveUser");

            if (role != "Chairman")
                return RedirectToPage("/LoginPage");

            var chairman = _context.Users
                .FirstOrDefault(u => u.FacultyId == facultyIdSession);

            if (chairman == null)
            {
                Message = "Chairman account not found.";
                return Page();
            }

            if (chairman.SchoolId == null || chairman.DepartmentId == null)
            {
                Message = "Chairman account has no School/Department assigned.";
                return Page();
            }

            bool exists = _context.Users.Any(u =>
                u.FacultyId == FacultyId ||
                u.Email == Email);

            if (exists)
            {
                Message = "Faculty ID or Email already exists.";
                return Page();
            }

            var teacher = new User
            {
                FacultyId = FacultyId,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = Password, // we'll hash later
                Role = "Teacher",
                Status = "Active",
                CreatedAt = DateTime.Now,
                MaxUnits = 21,

                SchoolId = chairman.SchoolId,
                DepartmentId = chairman.DepartmentId,
                CreatedBy = chairman.Id,
                CreatedByRole = "Chairman"
            };

            _context.Users.Add(teacher);
            _context.SaveChanges();

            Message = "Teacher registered successfully.";
            ModelState.Clear();

            FacultyId = "";
            FirstName = "";
            LastName = "";
            Email = "";
            Password = "";

            return Page();
        }
    }
}
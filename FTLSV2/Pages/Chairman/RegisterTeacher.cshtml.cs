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

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

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

            // Normalizing inputs
            FacultyId = FacultyId.Trim();
            FirstName = FirstName.Trim();
            LastName = LastName.Trim();
            Email = Email.Trim().ToLower();

            if (!System.Text.RegularExpressions.Regex.IsMatch(FacultyId, @"^\d{8}$"))
            {
                Message = "Faculty ID must be exactly 8 digits.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                Message = "Passwords do not match.";
                return Page();
            }

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

            bool facultyExists = _context.Users.Any(u => u.FacultyId == FacultyId);
            bool emailExists = _context.Users.Any(u => u.Email == Email);

            if (facultyExists)
            {
                Message = "Faculty ID already exists.";
                return Page();
            }

            if (emailExists)
            {
                Message = "Email already exists.";
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
                CreatedAt = DateTime.UtcNow,
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
            ConfirmPassword = "";

            return Page();
        }
    }
}
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FTLSV2.Pages
{
    public class RegisterPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public RegisterPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Faculty ID is required.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Faculty ID must be exactly 8 characters.")]
        public string FacultyId { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Confirm password is required.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            FacultyId = FacultyId?.Trim() ?? string.Empty;
            FirstName = FirstName?.Trim() ?? string.Empty;
            LastName = LastName?.Trim() ?? string.Empty;
            Email = Email?.Trim().ToLower() ?? string.Empty;
            Role = Role?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Role != "Teacher" && Role != "Chairman")
            {
                ErrorMessage = "Invalid role selected.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            bool facultyIdExists = _context.Users.Any(u => u.FacultyId == FacultyId);

            if (facultyIdExists)
            {
                ErrorMessage = "Faculty ID already exists.";
                return Page();
            }

            bool emailExists = _context.Users.Any(u => u.Email == Email);

            if (emailExists)
            {
                ErrorMessage = "Email already exists.";
                return Page();
            }

            var newUser = new User
            {
                FacultyId = FacultyId,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Role = Role,
                Password = Password,

                // Better than instantly allowing access.
                // Admin/Chairman should approve later.
                Status = "Pending",

                MaxUnits = 24,
                CurrentUnits = 0,

                SchoolId = null,
                DepartmentId = null,

                CreatedBy = null,
                CreatedByRole = "Self-Registration",

                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            SuccessMessage = "Registration submitted successfully. Please wait for account approval before logging in.";

            ModelState.Clear();

            FacultyId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Role = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            return Page();
        }
    }
}
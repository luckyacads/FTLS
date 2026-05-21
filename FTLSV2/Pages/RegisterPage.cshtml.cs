using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System;

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
        // Property name now matches asp-for="FacultyId" in your HTML
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
        [Required(ErrorMessage = "Department is required.")]
        public int? DepartmentId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "School is required.")]
        public int? SchoolId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Confirm password is required.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public List<Department> AvailableDepartments { get; set; } = new List<Department>();
        public List<School> AvailableSchools { get; set; } = new List<School>();

        public void OnGet()
        {
            AvailableDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
            AvailableSchools = _context.Schools.OrderBy(s => s.Name).ToList();
        }

        public IActionResult OnPost()
        {
            FirstName = FirstName?.Trim() ?? string.Empty;
            LastName = LastName?.Trim() ?? string.Empty;
            Email = Email?.Trim().ToLower() ?? string.Empty;

            AvailableDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
            AvailableSchools = _context.Schools.OrderBy(s => s.Name).ToList();

            // Validate the Faculty ID
            if (!int.TryParse(FacultyId, out int parsedFacultyId) || FacultyId.Length != 5)
            {
                ErrorMessage = "Faculty ID must be exactly 5 digits.";
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            // Database checks
            bool facultyIdExists = _context.Users.Any(u => u.FacultyId == parsedFacultyId);
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
                FacultyId = parsedFacultyId,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = Password,
                Role = "Faculty",
                DepartmentId = DepartmentId,
                SchoolId = SchoolId,
                Status = "Pending",
                MaxUnits = 24,
                CurrentUnits = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            SuccessMessage = "Registration submitted successfully. Please wait for account approval.";

            ModelState.Clear();
            FacultyId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            return Page();
        }
    }
}
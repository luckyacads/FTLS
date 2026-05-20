using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

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
        [StringLength(5, MinimumLength = 5, ErrorMessage = "Faculty ID must be exactly 5 digits.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Faculty ID must contain numbers only.")]
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

        // NEW: Changed from Role (string) to DepartmentId (int)
        [BindProperty]
        [Required(ErrorMessage = "Department is required.")]
        public int? DepartmentId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Confirm password is required.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        // NEW: A list to hold the departments from the database
        public List<Department> AvailableDepartments { get; set; } = new List<Department>();

        public void OnGet()
        {
            // Fetch departments from database and sort them alphabetically by Name
            AvailableDepartments = _context.Departments.OrderBy(d => d.Name).ToList();
        }

        public IActionResult OnPost()
        {
            FacultyId = FacultyId?.Trim() ?? string.Empty;
            FirstName = FirstName?.Trim() ?? string.Empty;
            LastName = LastName?.Trim() ?? string.Empty;
            Email = Email?.Trim().ToLower() ?? string.Empty;

            // Re-fetch departments in case the page reloads due to an error
            AvailableDepartments = _context.Departments.OrderBy(d => d.Name).ToList();

            if (!ModelState.IsValid)
            {
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
                Password = Password,

                // HARDCODED ROLE: Self-registered users default to Faculty
                Role = "Faculty",

                // SAVE THE CHOSEN DEPARTMENT
                DepartmentId = DepartmentId,

                Status = "Pending",
                MaxUnits = 24,
                CurrentUnits = 0,
                SchoolId = null,
                CreatedByRole = "Self-Registration",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            SuccessMessage = "Registration submitted successfully. Please wait for account approval before logging in.";

            ModelState.Clear();
            FacultyId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            DepartmentId = null;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            return Page();
        }
    }
}
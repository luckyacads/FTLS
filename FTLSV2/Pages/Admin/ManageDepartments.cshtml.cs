using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages.Admin
{
    public class ManageDepartmentsModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public ManageDepartmentsModel(FtlsDbContext context)
        {
            _context = context;
        }

        // Lists to display in the UI
        public IList<Department> Departments { get; set; } = new List<Department>();
        public IList<School> Schools { get; set; } = new List<School>();

        // Form Inputs
        [BindProperty] public int NewSchoolId { get; set; }
        [BindProperty] public string NewCode { get; set; }
        [BindProperty] public string NewName { get; set; }

        public IActionResult OnGet()
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userRole != "Admin")
            {
                return RedirectToPage("/LoginPage");
            }

            LoadPageData();

            return Page();
        }

        public IActionResult OnPostAddDepartment()
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userRole != "Admin")
            {
                return RedirectToPage("/LoginPage");
            }

            string normalizedCode = NewCode?.Trim().ToUpper();
            string normalizedName = NewName?.Trim();

            if (NewSchoolId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a school.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                TempData["ErrorMessage"] = "Department code is required.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                TempData["ErrorMessage"] = "Department name is required.";
                return RedirectToPage();
            }

            bool schoolExists = _context.Schools.Any(s => s.Id == NewSchoolId);

            if (!schoolExists)
            {
                TempData["ErrorMessage"] = "Selected school does not exist.";
                return RedirectToPage();
            }

            bool duplicateCode = _context.Departments
                .Any(d => d.Code.ToLower() == normalizedCode.ToLower());

            if (duplicateCode)
            {
                TempData["ErrorMessage"] = $"Department code '{normalizedCode}' already exists.";
                return RedirectToPage();
            }

            bool duplicateName = _context.Departments
                .Any(d => d.Name.ToLower() == normalizedName.ToLower());

            if (duplicateName)
            {
                TempData["ErrorMessage"] = $"Department name '{normalizedName}' already exists.";
                return RedirectToPage();
            }

            var newDept = new Department
            {
                SchoolId = NewSchoolId,
                Code = normalizedCode,
                Name = normalizedName
            };

            try
            {
                _context.Departments.Add(newDept);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"{normalizedCode} Department created successfully!";
            }
            catch (Exception ex)
            {
                var baseMessage = ex.GetBaseException()?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"An error occurred while creating the department. {baseMessage}";
            }

            return RedirectToPage();
        }

        private void LoadPageData()
        {
            Departments = _context.Departments
                .OrderBy(d => d.Code)
                .ToList();

            Schools = _context.Schools
                .OrderBy(s => s.Name)
                .ToList();
        }
    }
}
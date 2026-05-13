using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
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
        public IList<Department> Departments { get; set; }
        public IList<School> Schools { get; set; }

        // Form Inputs
        [BindProperty] public int NewSchoolId { get; set; }
        [BindProperty] public string NewCode { get; set; }
        [BindProperty] public string NewName { get; set; }

        public IActionResult OnGet()
        {
            // Security check to ensure only Admins can access this
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToPage("/LoginPage");
            }

            // Load all departments and schools to display on the page
            Departments = _context.Departments.OrderBy(d => d.Code).ToList();
            Schools = _context.Schools.OrderBy(s => s.Name).ToList();

            return Page();
        }

        public IActionResult OnPostAddDepartment()
        {
            if (!string.IsNullOrEmpty(NewCode) && !string.IsNullOrEmpty(NewName) && NewSchoolId > 0)
            {
                var newDept = new Department
                {
                    SchoolId = NewSchoolId,
                    Code = NewCode.ToUpper(), // Standardize codes to uppercase
                    Name = NewName

                };

                _context.Departments.Add(newDept);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"{NewCode} Department created successfully!";
            }

            return RedirectToPage();
        }
    }
}
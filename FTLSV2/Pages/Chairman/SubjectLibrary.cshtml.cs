using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;

namespace FTLSV2.Pages.Admin
{
    public class SubjectLibraryModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public SubjectLibraryModel(FtlsDbContext db)
        {
            _db = db;
        }

        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<Department> Departments { get; set; } = new List<Department>();

        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public string NewSubjectDepartment { get; set; }

        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public string EditSubjectDepartment { get; set; }

        public void OnGet()
        {
            Subjects = _db.Subjects
                .Include(s => s.Department)
                .OrderBy(s => s.Title)
                .ToList();

            Departments = _db.Departments.OrderBy(d => d.Name).ToList();
        }

        public IActionResult OnPostAddSubject()
        {
            if (!string.IsNullOrEmpty(NewSubjectCode) && !string.IsNullOrEmpty(NewSubjectTitle) && !string.IsNullOrEmpty(NewSubjectDepartment))
            {
                string deptName = NewSubjectDepartment.Trim();
                var dept = _db.Departments.FirstOrDefault(d => d.Name.ToLower() == deptName.ToLower());

                if (dept == null)
                {
                    dept = new Department { Name = deptName };
                    _db.Departments.Add(dept);
                    _db.SaveChanges();
                }

                var subject = new Subject
                {
                    Code = NewSubjectCode,
                    Title = NewSubjectTitle,
                    Units = NewSubjectUnits,
                    DepartmentId = dept.Id
                };

                try
                {
                    _db.Subjects.Add(subject);
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Subject successfully added!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error adding subject: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while saving the subject. {baseMsg}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostEditSubject()
        {
            var subjectToEdit = _db.Subjects.FirstOrDefault(s => s.SubjectId == EditSubjectId);
            if (subjectToEdit != null && !string.IsNullOrEmpty(EditSubjectDepartment))
            {
                string deptName = EditSubjectDepartment.Trim();
                var dept = _db.Departments.FirstOrDefault(d => d.Name.ToLower() == deptName.ToLower());

                if (dept == null)
                {
                    dept = new Department { Name = deptName };
                    _db.Departments.Add(dept);
                    _db.SaveChanges();
                }

                subjectToEdit.Code = EditSubjectCode;
                subjectToEdit.Title = EditSubjectTitle;
                subjectToEdit.Units = EditSubjectUnits;
                subjectToEdit.DepartmentId = dept.Id;

                try
                {
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Subject successfully updated!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error updating subject: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while updating the subject. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteSubject(int subjectId)
        {
            var subject = _db.Subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            if (subject != null)
            {
                try
                {
                    _db.Subjects.Remove(subject);
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Subject successfully deleted!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error deleting subject: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while deleting the subject. {baseMsg}";
                }
            }
            return RedirectToPage();
        }
    }
}
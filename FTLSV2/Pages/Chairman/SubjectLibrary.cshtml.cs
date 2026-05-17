using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public string NewSubjectDepartment { get; set; }
        [BindProperty] public string NewSubjectSemester { get; set; }

        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public string EditSubjectDepartment { get; set; }
        [BindProperty] public string EditSubjectSemester { get; set; }

        public void OnGet()
        {
            Subjects = _db.Subjects.OrderBy(s => s.Title).ToList();
        }

        public IActionResult OnPostAddSubject()
        {
            if (!string.IsNullOrEmpty(NewSubjectCode) && !string.IsNullOrEmpty(NewSubjectTitle))
            {
                var subject = new Subject
                {
                    Code = NewSubjectCode,
                    Title = NewSubjectTitle,
                    Units = NewSubjectUnits,
                    Department = NewSubjectDepartment,
                    Semester = NewSubjectSemester
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
            if (subjectToEdit != null)
            {
                subjectToEdit.Code = EditSubjectCode;
                subjectToEdit.Title = EditSubjectTitle;
                subjectToEdit.Units = EditSubjectUnits;
                subjectToEdit.Department = EditSubjectDepartment;
                subjectToEdit.Semester = EditSubjectSemester;

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
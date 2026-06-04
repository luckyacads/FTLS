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

        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }

        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }

        public void OnGet()
        {
            Subjects = _db.Subjects
                .OrderBy(s => s.Title)
                .ToList();
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
                    DepartmentId = null
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
                subjectToEdit.DepartmentId = null;

                try
                {
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Subject successfully updated!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    TempData["ErrorMessage"] = $"An error occurred while updating the subject. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        // --- DELETED (SOFT DELETE) ---
        public IActionResult OnPostDeleteSubject(int subjectId)
        {
            var subject = _db.Subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            if (subject != null)
            {
                try
                {
                    subject.Is_delete = true; // Flips the flag instead of removing
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = $"{subject.Code} has been flagged as deleted!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    TempData["ErrorMessage"] = $"An error occurred while deleting the subject. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        // --- RESTORE (REVERSE SOFT DELETE) ---
        public IActionResult OnPostRestoreSubject(int subjectId)
        {
            var subject = _db.Subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            if (subject != null)
            {
                try
                {
                    subject.Is_delete = false; // Flips it back to active
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = $"{subject.Code} has been successfully restored!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    TempData["ErrorMessage"] = $"An error occurred while restoring the subject. {baseMsg}";
                }
            }
            return RedirectToPage();
        }
    }
}
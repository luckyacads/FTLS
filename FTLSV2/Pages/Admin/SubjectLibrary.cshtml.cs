using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Subjects loaded from the database
        public List<Subject> Subjects { get; set; } = new List<Subject>();

        // --- PROPERTIES FOR ADDING A SUBJECT ---
        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public string NewSubjectDepartment { get; set; }
        [BindProperty] public string NewSubjectSemester { get; set; } // <-- NEW

        // --- PROPERTIES FOR EDITING A SUBJECT ---
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public string EditSubjectDepartment { get; set; }
        [BindProperty] public string EditSubjectSemester { get; set; } // <-- NEW

        public void OnGet()
        {
            Subjects = _db.Subjects.OrderBy(s => s.Title).ToList();
        }

        // 2. Add Subject Method
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
                    Semester = NewSubjectSemester // <-- Save to DB
                };

                _db.Subjects.Add(subject);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Subject successfully added!";
            }
            return RedirectToPage();
        }

        // 3. Edit Subject Method
        public IActionResult OnPostEditSubject()
        {
            var subjectToEdit = _db.Subjects.FirstOrDefault(s => s.SubjectId == EditSubjectId);
            if (subjectToEdit != null)
            {
                subjectToEdit.Code = EditSubjectCode;
                subjectToEdit.Title = EditSubjectTitle;
                subjectToEdit.Units = EditSubjectUnits;
                subjectToEdit.Department = EditSubjectDepartment;
                subjectToEdit.Semester = EditSubjectSemester; // <-- Save to DB

                _db.SaveChanges();
                TempData["SuccessMessage"] = "Subject successfully updated!";
            }
            return RedirectToPage();
        }

        // 4. Delete Subject Method
        public IActionResult OnPostDeleteSubject(int subjectId)
        {
            var subject = _db.Subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            if (subject != null)
            {
                _db.Subjects.Remove(subject);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Subject successfully deleted!";
            }
            return RedirectToPage();
        }
    }
}
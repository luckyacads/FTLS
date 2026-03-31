using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages
{
    public class SubjectLibraryModel : PageModel
    {
        private readonly Data.FtlsDbContext _db;

        public SubjectLibraryModel(Data.FtlsDbContext db)
        {
            _db = db;
        }

        // Subjects loaded from the database
        public List<Models.Subject> Subjects { get; set; } = new List<Models.Subject>();

        // --- PROPERTIES FOR ADDING A SUBJECT ---
        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public string NewSubjectDepartment { get; set; }

        // --- PROPERTIES FOR EDITING A SUBJECT ---
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public string EditSubjectDepartment { get; set; }

        public void OnGet()
        {
            Subjects = _db.Subjects.OrderBy(s => s.Title).ToList();
        }

        // 2. Add Subject Method
        public IActionResult OnPostAddSubject()
        {
            if (!string.IsNullOrEmpty(NewSubjectCode) && !string.IsNullOrEmpty(NewSubjectTitle))
            {
                var subject = new Models.Subject
                {
                    Code = NewSubjectCode,
                    Title = NewSubjectTitle,
                    Units = NewSubjectUnits,
                    Department = NewSubjectDepartment
                };

                _db.Subjects.Add(subject);
                _db.SaveChanges();

                // 2. NEW: Send a record to the Audit Logs if available
                try
                {
                    AuditLogsModel.Logs.Insert(0, new LogEntry
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        Username = "Admin",
                        Action = $"Created subject: {NewSubjectCode}"
                    });
                }
                catch
                {
                    // ignore if audit log model is not present
                }

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

                _db.SaveChanges();
                TempData["SuccessMessage"] = "Subject successfully updated!";
                // Log the update
                try
                {
                    AuditLogsModel.Logs.Insert(0, new LogEntry
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        Username = "Admin",
                        Action = $"Updated subject: {EditSubjectCode}"
                    });
                }
                catch
                {
                    // ignore if audit log model is not present
                }
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
                // Log the deletion
                try
                {
                    AuditLogsModel.Logs.Insert(0, new LogEntry
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        Username = "Admin",
                        Action = $"Deleted subject: {subject.Code}"
                    });
                }
                catch
                {
                    // ignore if audit log model is not present
                }
            }
            return RedirectToPage();
        }
    }

    // Note: use Models.Subject for the EF-backed subject entity
}
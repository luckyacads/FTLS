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

                try
                {
                    _db.Subjects.Add(subject);
                    AddAuditEntryToContext($"Created subject: {NewSubjectCode} - {NewSubjectTitle}");
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

                try
                {
                    AddAuditEntryToContext($"Updated subject: {EditSubjectCode} - {EditSubjectTitle}");
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

        // 4. Delete Subject Method
        public IActionResult OnPostDeleteSubject(int subjectId)
        {
            var subject = _db.Subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            if (subject != null)
            {
                try
                {
                    _db.Subjects.Remove(subject);
                    AddAuditEntryToContext($"Deleted subject: {subject.Code} - {subject.Title}");
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

        private void AddAuditEntryToContext(string action)
        {
            // Get the currently logged-in user's FacultyId from session
            var facultyId = HttpContext.Session.GetString("ActiveUser");
            int? userId = null;

            if (!string.IsNullOrEmpty(facultyId))
            {
                // Look up the user ID from the database using FacultyId
                var user = _db.Users.FirstOrDefault(u => u.FacultyId == facultyId);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var auditLog = new AuditLog
            {
                // Use UTC to match PostgreSQL 'timestamp with time zone' requirements
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Action = action
            };

            // Add to context but do NOT call SaveChanges here. Caller will persist.
            _db.AuditLogs.Add(auditLog);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages
{
    public class SubjectLibraryModel : PageModel
    {
        // 1. Temporary list to store our subjects
        public static List<SubjectModel> Subjects { get; set; } = new List<SubjectModel>
        {
            new SubjectModel { Id = Guid.NewGuid().ToString(), Code = "CS101", Title = "Introduction to Programming", Units = 3, Department = "Computer Science" },
            new SubjectModel { Id = Guid.NewGuid().ToString(), Code = "MATH201", Title = "Calculus II", Units = 4, Department = "Mathematics" }
        };

        // --- PROPERTIES FOR ADDING A SUBJECT ---
        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public string NewSubjectDepartment { get; set; }

        // --- PROPERTIES FOR EDITING A SUBJECT ---
        [BindProperty] public string EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public string EditSubjectDepartment { get; set; }

        public void OnGet()
        {
        }

        // 2. Add Subject Method
        public IActionResult OnPostAddSubject()
        {
            if (!string.IsNullOrEmpty(NewSubjectCode) && !string.IsNullOrEmpty(NewSubjectTitle))
            {
                // 1. Add the subject to the Subject list
                Subjects.Add(new SubjectModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = NewSubjectCode,
                    Title = NewSubjectTitle,
                    Units = NewSubjectUnits,
                    Department = NewSubjectDepartment
                });

                // 2. NEW: Send a record to the Audit Logs!
                AuditLogsModel.Logs.Insert(0, new LogEntry
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    Username = "Admin",
                    Action = $"Created subject: {NewSubjectCode}",
                    IPAddress = "127.0.0.1"
                });

                TempData["SuccessMessage"] = "Subject successfully added!";
            }
            return RedirectToPage();
        }

        // 3. Edit Subject Method
        public IActionResult OnPostEditSubject()
        {
            var subjectToEdit = Subjects.FirstOrDefault(s => s.Id == EditSubjectId);
            if (subjectToEdit != null)
            {
                subjectToEdit.Code = EditSubjectCode;
                subjectToEdit.Title = EditSubjectTitle;
                subjectToEdit.Units = EditSubjectUnits;
                subjectToEdit.Department = EditSubjectDepartment;

                TempData["SuccessMessage"] = "Subject successfully updated!";
            }
            return RedirectToPage();
        }
    }

    // Class defining what a Subject looks like
    public class SubjectModel
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public int Units { get; set; }
        public string Department { get; set; }
    }
}
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
        public Dictionary<int, List<Curriculum>> SubjectCurriculums { get; set; } = new Dictionary<int, List<Curriculum>>();

        [BindProperty] public string NewSubjectCode { get; set; }
        [BindProperty] public string NewSubjectTitle { get; set; }
        [BindProperty] public int NewSubjectUnits { get; set; }
        [BindProperty] public int? NewSubjectDepartmentId { get; set; }
        [BindProperty] public string NewSubjectYearLevel { get; set; }
        [BindProperty] public string NewSubjectSemester { get; set; }
        [BindProperty] public string NewSubjectCurriculumYear { get; set; }

        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public string EditSubjectCode { get; set; }
        [BindProperty] public string EditSubjectTitle { get; set; }
        [BindProperty] public int EditSubjectUnits { get; set; }
        [BindProperty] public int? EditSubjectDepartmentId { get; set; }
        [BindProperty] public string EditSubjectYearLevel { get; set; }
        [BindProperty] public string EditSubjectSemester { get; set; }
        [BindProperty] public string EditSubjectCurriculumYear { get; set; }

        // Binds the URL query string parameter dynamically (?showDeleted=true)
        [BindProperty(SupportsGet = true)]
        public bool ShowDeleted { get; set; }

        public void OnGet()
        {
            // Filter records based on whether the view state targets soft-deleted entries
            Subjects = _db.Subjects
                .Where(s => s.Is_delete == ShowDeleted)
                .OrderBy(s => s.Title)
                .ToList();

            var subjectIds = Subjects.Select(s => s.SubjectId).ToList();
            SubjectCurriculums = _db.Curriculums
                .Where(c => subjectIds.Contains(c.SubjectId))
                .AsEnumerable()
                .GroupBy(c => c.SubjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Departments = _db.Departments.OrderBy(d => d.Name).ToList();
        }

        public IActionResult OnPostAddSubject()
        {
            if (!string.IsNullOrEmpty(NewSubjectCode) && !string.IsNullOrEmpty(NewSubjectTitle) && NewSubjectDepartmentId.HasValue && !string.IsNullOrEmpty(NewSubjectYearLevel) && !string.IsNullOrEmpty(NewSubjectSemester))
            {
                var subject = new Subject
                {
                    Code = NewSubjectCode,
                    Title = NewSubjectTitle,
                    Units = NewSubjectUnits,
                    DepartmentId = NewSubjectDepartmentId
                };

                try
                {
                    _db.Subjects.Add(subject);
                    _db.SaveChanges();

                    var curriculum = new Curriculum
                    {
                        SubjectId = subject.SubjectId,
                        DepartmentId = NewSubjectDepartmentId.Value,
                        YearLevel = NewSubjectYearLevel,
                        Semester = NewSubjectSemester,
                        CurriculumYear = NewSubjectCurriculumYear ?? "2023"
                    };
                    _db.Curriculums.Add(curriculum);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Subject successfully added and mapped to curriculum!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    TempData["ErrorMessage"] = $"An error occurred while saving the subject. {baseMsg}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all required fields, including Department, Year Level, and Semester.";
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
                subjectToEdit.DepartmentId = EditSubjectDepartmentId;

                try
                {
                    var curriculum = _db.Curriculums.FirstOrDefault(c => c.SubjectId == subjectToEdit.SubjectId);
                    if (curriculum == null)
                    {
                        curriculum = new Curriculum
                        {
                            SubjectId = subjectToEdit.SubjectId
                        };
                        _db.Curriculums.Add(curriculum);
                    }

                    if (EditSubjectDepartmentId.HasValue)
                    {
                        curriculum.DepartmentId = EditSubjectDepartmentId.Value;
                    }
                    curriculum.YearLevel = EditSubjectYearLevel ?? "";
                    curriculum.Semester = EditSubjectSemester ?? "";
                    curriculum.CurriculumYear = EditSubjectCurriculumYear ?? "2023";

                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Subject and curriculum successfully updated!";
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
                    subject.Is_delete = true;
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
                    subject.Is_delete = false;
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
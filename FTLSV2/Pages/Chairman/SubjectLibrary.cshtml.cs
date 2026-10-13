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
        [BindProperty] public int EditCurriculumId { get; set; }

        // Binds the URL query string parameter dynamically (?showDeleted=true)
        [BindProperty(SupportsGet = true)]
        public bool ShowDeleted { get; set; }

        public void OnGet()
        {
            // Filter records based on whether the view state targets soft-deleted entries
            Subjects = _db.Subjects
                .Include(s => s.Department)
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
                try
                {
                    var cleanCode = NewSubjectCode.Trim();

                    // Enforce unique subject code rule
                    var existingSubject = _db.Subjects.FirstOrDefault(s => s.Code.ToLower() == cleanCode.ToLower());
                    if (existingSubject != null)
                    {
                        if (!existingSubject.Is_delete)
                        {
                            TempData["ErrorMessage"] = $"Subject code '{cleanCode}' is already in use by '{existingSubject.Title}'. Subject codes must be unique.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = $"Subject code '{cleanCode}' belongs to a deleted subject. Please restore it or use a different code.";
                        }
                        return RedirectToPage();
                    }

                    var subject = new Subject
                    {
                        Code = cleanCode,
                        Title = NewSubjectTitle.Trim(),
                        Units = NewSubjectUnits,
                        DepartmentId = NewSubjectDepartmentId
                    };
                    _db.Subjects.Add(subject);
                    _db.SaveChanges();

                    // Add curriculum mapping
                    var targetYear = NewSubjectCurriculumYear ?? "2023";
                    var curriculum = new Curriculum
                    {
                        SubjectId = subject.SubjectId,
                        DepartmentId = NewSubjectDepartmentId.Value,
                        YearLevel = NewSubjectYearLevel,
                        Semester = NewSubjectSemester,
                        CurriculumYear = targetYear
                    };
                    _db.Curriculums.Add(curriculum);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = $"Subject '{cleanCode}' successfully added to the curriculum!";
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
                var cleanCode = (EditSubjectCode ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(cleanCode))
                {
                    TempData["ErrorMessage"] = "Subject code cannot be empty.";
                    return RedirectToPage();
                }

                // Enforce unique subject code rule across other subjects
                var duplicateSubject = _db.Subjects.FirstOrDefault(s => s.SubjectId != EditSubjectId && s.Code.ToLower() == cleanCode.ToLower());
                if (duplicateSubject != null)
                {
                    if (!duplicateSubject.Is_delete)
                    {
                        TempData["ErrorMessage"] = $"Subject code '{cleanCode}' is already in use by '{duplicateSubject.Title}'. Subject codes must be unique.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Subject code '{cleanCode}' matches a deleted subject. Subject codes must be unique.";
                    }
                    return RedirectToPage();
                }

                subjectToEdit.Code = cleanCode;
                subjectToEdit.Title = (EditSubjectTitle ?? string.Empty).Trim();
                subjectToEdit.Units = EditSubjectUnits;
                subjectToEdit.DepartmentId = EditSubjectDepartmentId;

                try
                {
                    // Find the specific curriculum mapping by EditCurriculumId if provided
                    var curriculum = EditCurriculumId > 0
                        ? _db.Curriculums.FirstOrDefault(c => c.CurriculumId == EditCurriculumId)
                        : null;

                    // Fallback to the first mapping if specific mapping is not found or not provided
                    if (curriculum == null)
                    {
                        curriculum = _db.Curriculums.FirstOrDefault(c => c.SubjectId == subjectToEdit.SubjectId);
                    }

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
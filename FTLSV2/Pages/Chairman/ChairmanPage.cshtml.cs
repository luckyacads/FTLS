using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages.Chairman
{
    public class ChairmanPageModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public ChairmanPageModel(FtlsDbContext context)
        {
            _context = context;
        }

        // --- DROPDOWN LISTS ---
        public IList<User> ActiveFaculty { get; set; } = new List<User>();
        public IList<Subject> ActiveSubjects { get; set; } = new List<Subject>();
        public IList<Room> AllRooms { get; set; } = new List<Room>();
        public IList<string> AvailableAcademicYears { get; set; } = new List<string>();

        // --- FORM INPUTS ---
        [BindProperty] public int SelectedFacultyId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }

        // CHANGED: SelectedDays is now a List to catch checkbox array
        [BindProperty] public List<string> SelectedDays { get; set; } = new List<string>();

        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }

        // ADDED: New property to catch the Semester dropdown
        [BindProperty] public string SelectedSemester { get; set; }

        [BindProperty] public int EditScheduleId { get; set; }
        [BindProperty] public int EditFacultyId { get; set; }
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public int EditRoomId { get; set; }

        // CHANGED: EditDays is now a List for checkboxes in the Edit Modal
        [BindProperty] public List<string> EditDays { get; set; } = new List<string>();

        [BindProperty] public TimeSpan EditStartTime { get; set; }
        [BindProperty] public TimeSpan EditEndTime { get; set; }
        [BindProperty] public int? EditOfferCode { get; set; }
        [BindProperty] public string EditAcademicYear { get; set; }

        // ADDED: New property for Semester in the Edit Modal
        [BindProperty] public string EditSemester { get; set; }

        [BindProperty] public int DeleteScheduleId { get; set; }
        public int GlobalMaxLimit { get; set; }

        public IList<AssignedLoad> CurrentSchedules { get; set; } = new List<AssignedLoad>();

        public IActionResult OnGet()
        {
            var activeUserString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeUserString)) return RedirectToPage("/LoginPage");

            LoadPageData();
            return Page();
        }

        public IActionResult OnGetAvailableRooms(string days, string startTime, string endTime, int? excludeScheduleId)
        {
            if (string.IsNullOrEmpty(days) || string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
            {
                return new JsonResult(new List<object>());
            }

            if (!TimeSpan.TryParse(startTime, out TimeSpan start) || !TimeSpan.TryParse(endTime, out TimeSpan end))
            {
                return new JsonResult(new List<object>());
            }

            var daysList = days.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(d => d.Trim())
                               .ToList();

            var allRooms = _context.Rooms.ToList();
            var allSchedules = _context.Schedules.ToList();

            var occupiedRoomIds = allSchedules
                .Where(s => s.RoomId.HasValue && (excludeScheduleId == null || s.ScheduleId != excludeScheduleId))
                .Where(s => CheckOverlap(s.TimeSlot, daysList, start, end))
                .Select(s => s.RoomId.Value)
                .Distinct()
                .ToList();

            var availableRooms = allRooms
                .Where(r => !occupiedRoomIds.Contains(r.RoomId))
                .Select(r => new {
                    roomId = r.RoomId,
                    name = r.Name
                })
                .ToList();

            return new JsonResult(availableRooms);
        }

        private bool CheckOverlap(string timeSlot1, List<string> days2, TimeSpan start2, TimeSpan end2)
        {
            if (string.IsNullOrWhiteSpace(timeSlot1)) return false;

            var parts = timeSlot1.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6) return false;

            int timeStartIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (char.IsDigit(parts[i][0]))
                {
                    timeStartIndex = i;
                    break;
                }
            }
            if (timeStartIndex <= 0) return false;

            var days1Part = string.Join(" ", parts.Take(timeStartIndex));
            var days1 = days1Part.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(d => d.Trim())
                                 .ToList();

            bool daysOverlap = days1.Any(d1 => days2.Contains(d1));
            if (!daysOverlap) return false;

            var timePart = string.Join(" ", parts.Skip(timeStartIndex));
            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                timePart,
                @"(\d{1,2}:\d{2}\s*[aApP][mM])\s*-\s*(\d{1,2}:\d{2}\s*[aApP][mM])",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (!timeMatch.Success) return false;

            if (DateTime.TryParse(timeMatch.Groups[1].Value, out DateTime s1) &&
                DateTime.TryParse(timeMatch.Groups[2].Value, out DateTime e1))
            {
                TimeSpan start1 = s1.TimeOfDay;
                TimeSpan end1 = e1.TimeOfDay;

                return start1 < end2 && start2 < end1;
            }

            return false;
        }

        public IActionResult OnPost()
        {
            var activeUserString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeUserString)) return RedirectToPage("/LoginPage");

            if (SelectedFacultyId == 0 || SelectedDays == null || SelectedDays.Count == 0 || SelectedSubjectId == 0 || string.IsNullOrEmpty(SelectedSemester))
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields, including days and semester.";
                LoadPageData();
                return Page();
            }

            if (EndTime <= StartTime)
            {
                TempData["ErrorMessage"] = "Invalid time range.";
                LoadPageData();
                return Page();
            }

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == SelectedFacultyId 
                            && s.AcademicYear == SelectedAcademicYear 
                            && s.Semester == SelectedSemester)
                .Sum(s => s.AssignedUnits);

            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds Max Overload limit. Current units for this semester: {currentTotalUnits}. This course units: {officialSubjectUnits}. Max limit: {globalMaxLimit}.";
                LoadPageData();
                return Page();
            }

            // CHANGED: Convert the List of selected days into a comma-separated string
            string joinedDays = string.Join(", ", SelectedDays);
            string formattedTimeSlot = $"{joinedDays} {DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";

            var newSchedule = new Schedule
            {
                FacultyId = SelectedFacultyId,
                SubjectId = SelectedSubjectId,
                RoomId = SelectedRoomId > 0 ? SelectedRoomId : null,
                TimeSlot = formattedTimeSlot,
                AssignedUnits = officialSubjectUnits,
                OfferCode = SelectedOfferCode,
                AcademicYear = SelectedAcademicYear,
                Semester = SelectedSemester // UPDATED to use dynamic semester dropdown
            };

            _context.Schedules.Add(newSchedule);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Schedule successfully assigned!";

            return RedirectToPage();
        }

        public IActionResult OnPostEditSchedule()
        {
            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == EditScheduleId);
            if (schedule == null) return Page();

            if (EditDays == null || EditDays.Count == 0)
            {
                TempData["ErrorMessage"] = "You must select at least one day.";
                return RedirectToPage();
            }

            if (EditEndTime <= EditStartTime)
            {
                TempData["ErrorMessage"] = "Invalid time range.";
                return RedirectToPage();
            }

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == EditSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == EditFacultyId 
                            && s.AcademicYear == EditAcademicYear 
                            && s.Semester == EditSemester
                            && s.ScheduleId != EditScheduleId)
                .Sum(s => s.AssignedUnits);

            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds Max Overload limit. Current units for this semester: {currentTotalUnits}. This course units: {officialSubjectUnits}. Max limit: {globalMaxLimit}.";
                return RedirectToPage();
            }

            schedule.FacultyId = EditFacultyId;
            schedule.SubjectId = EditSubjectId;
            schedule.RoomId = EditRoomId > 0 ? EditRoomId : null;
            schedule.AssignedUnits = officialSubjectUnits;

            string joinedEditDays = string.Join(", ", EditDays);
            schedule.TimeSlot = $"{joinedEditDays} {DateTime.Today.Add(EditStartTime):h:mm tt} - {DateTime.Today.Add(EditEndTime):h:mm tt}";

            schedule.OfferCode = EditOfferCode;
            schedule.AcademicYear = EditAcademicYear;
            schedule.Semester = EditSemester;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Schedule successfully updated!";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteSchedule()
        {
            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == DeleteScheduleId);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                _context.SaveChanges();
            }
            return RedirectToPage();
        }

        private void LoadPageData()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 21;

            ActiveFaculty = _context.Users.Where(u => u.Role == "Faculty" && u.Is_delete == false).ToList(); // Ensure deleted faculty aren't shown
            ActiveSubjects = _context.Subjects.ToList();
            AllRooms = _context.Rooms.ToList();

            AvailableAcademicYears = new List<string>
            {
                "2023-2024",
                "2024-2025",
                "2025-2026",
                "2026-2027"
            };

            CurrentSchedules = _context.Schedules.Select(s => new AssignedLoad
            {
                ScheduleId = s.ScheduleId,
                FacultyId = s.FacultyId,
                FacultyName = "Engr. " + s.Faculty.FirstName + " " + s.Faculty.LastName,
                CourseCode = s.Subject.Code,
                SubjectTitle = s.Subject.Title,
                Schedule = s.TimeSlot,
                RoomName = s.Room != null ? s.Room.Name : "No room",
                OfferCode = s.OfferCode,
                AcademicYear = s.AcademicYear,
                Semester = s.Semester,
                SubjectId = s.SubjectId,
                RoomId = s.RoomId
            }).ToList();
        }

        public class AssignedLoad
        {
            public int ScheduleId { get; set; }
            public int FacultyId { get; set; }
            public string FacultyName { get; set; }
            public string CourseCode { get; set; }
            public string SubjectTitle { get; set; }
            public string Schedule { get; set; }
            public string RoomName { get; set; }
            public int? OfferCode { get; set; }
            public string AcademicYear { get; set; }
            public string Semester { get; set; }
            public int SubjectId { get; set; }
            public int? RoomId { get; set; }
        }
    }
}
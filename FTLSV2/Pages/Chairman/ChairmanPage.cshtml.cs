using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        // --- FACULTY PROFILE LOAD POPUP MODEL ---
        public bool ShowFacultyLoadPopup { get; set; } = false;
        public List<FacultyLoadProfileView> FacultyProfilesLoadList { get; set; } = new List<FacultyLoadProfileView>();

        public record FacultyLoadProfileView
        {
            public int FacultyId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string EmploymentType { get; set; }
            public int MaxUnits { get; set; }
            public int TotalAssignedUnits { get; set; }
            public bool IsUnderloaded { get; set; }
            public bool IsOverloaded { get; set; }
            public List<AssignedLoad> Schedules { get; set; } = new List<AssignedLoad>();
        }

        // --- DROPDOWN LISTS ---
        public IList<User> ActiveFaculty { get; set; } = new List<User>();
        public IList<Subject> ActiveSubjects { get; set; } = new List<Subject>();
        public IList<Room> AllRooms { get; set; } = new List<Room>();
        public IList<string> AvailableAcademicYears { get; set; } = new List<string>();

        public record FacultyAlertView(int FacultyId, string FacultyName, string AcademicYear, int AssignedUnits, int LimitUnits, string Message, string AlertType);
        public List<FacultyAlertView> FacultyAlerts { get; set; } = new List<FacultyAlertView>();

        // --- FORM INPUTS ---
        [BindProperty] public int SelectedFacultyId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }
        [BindProperty] public List<string> SelectedDays { get; set; } = new List<string>();
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }
        [BindProperty] public string SelectedSemester { get; set; }

        [BindProperty] public int EditScheduleId { get; set; }
        [BindProperty] public int EditFacultyId { get; set; }
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public int EditRoomId { get; set; }
        [BindProperty] public List<string> EditDays { get; set; } = new List<string>();
        [BindProperty] public TimeSpan EditStartTime { get; set; }
        [BindProperty] public TimeSpan EditEndTime { get; set; }
        [BindProperty] public int? EditOfferCode { get; set; }
        [BindProperty] public string EditAcademicYear { get; set; }
        [BindProperty] public string EditSemester { get; set; }

        [BindProperty] public int DeleteScheduleId { get; set; }
        public int GlobalMaxLimit { get; set; }

        public IList<AssignedLoad> CurrentSchedules { get; set; } = new List<AssignedLoad>();

        public IActionResult OnGet()
        {
            var activeUserString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeUserString)) return RedirectToPage("/LoginPage");

            LoadPageData();

            ShowFacultyLoadPopup = true;

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
                            && s.AcademicYear == SelectedAcademicYear)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.FirstOrDefault(u => u.FacultyId == SelectedFacultyId);
            int facultyMaxUnits = faculty?.MaxUnits ?? 18;

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > facultyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({facultyMaxUnits} units) for Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Added course: {officialSubjectUnits} units.";
                LoadPageData();
                return Page();
            }

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
                Semester = SelectedSemester
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
                            && s.ScheduleId != EditScheduleId)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.FirstOrDefault(u => u.FacultyId == EditFacultyId);
            int facultyMaxUnits = faculty?.MaxUnits ?? 18;

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > facultyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({facultyMaxUnits} units) for Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Course: {officialSubjectUnits} units.";
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
            GlobalMaxLimit = settings?.MaxOverload ?? 30;

            ActiveFaculty = _context.Users.Where(u => u.Role == "Faculty" && u.Is_delete == false).ToList();
            ActiveSubjects = _context.Subjects.Where(s => !s.Is_delete).ToList();
            AllRooms = _context.Rooms.ToList();

            // Calculate current Academic Year dynamically
            int currentYear = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string currentAY = (month >= 1 && month <= 5) ? $"{currentYear - 1}-{currentYear}" : $"{currentYear}-{currentYear + 1}";
            SelectedAcademicYear = currentAY;

            // Get schedules specifically for the current Academic Year
            var currentYearSchedules = _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Where(s => s.AcademicYear == currentAY)
                .ToList();

            FacultyProfilesLoadList = ActiveFaculty.Select(f => {
                var facSchedules = currentYearSchedules.Where(s => s.FacultyId == f.FacultyId).Select(s => new AssignedLoad
                {
                    ScheduleId = s.ScheduleId,
                    FacultyId = s.FacultyId,
                    CourseCode = s.Subject?.Code ?? "",
                    SubjectTitle = s.Subject?.Title ?? "",
                    Schedule = s.TimeSlot,
                    RoomName = s.Room?.Name ?? "No room",
                    OfferCode = s.OfferCode,
                    AcademicYear = s.AcademicYear,
                    Semester = s.Semester,
                    AssignedUnits = s.AssignedUnits
                }).ToList();

                int totalUnits = facSchedules.Sum(s => s.AssignedUnits);
                int facultyMaxUnits = f.MaxUnits; // Reads directly from User database table
                bool isFullTime = facultyMaxUnits >= 18;
                string empType = isFullTime ? "Full-Time" : "Part-Time";

                bool isUnder = isFullTime && totalUnits < 18;
                bool isOver = totalUnits > facultyMaxUnits;

                return new FacultyLoadProfileView
                {
                    FacultyId = f.FacultyId,
                    FirstName = f.FirstName,
                    LastName = f.LastName,
                    Email = f.Email,
                    EmploymentType = empType,
                    MaxUnits = facultyMaxUnits,
                    TotalAssignedUnits = totalUnits,
                    IsUnderloaded = isUnder,
                    IsOverloaded = isOver,
                    Schedules = facSchedules
                };
            }).ToList();

            // --- CALCULATE ALL FACULTY ALERTS (UNDERLOAD & OVERLOAD) ---
            FacultyAlerts = new List<FacultyAlertView>();

            foreach (var faculty in ActiveFaculty)
            {
                int facultyMaxUnits = faculty.MaxUnits; // Reads directly from DB
                bool isFullTime = facultyMaxUnits >= 18;
                string empType = isFullTime ? "Full-Time" : "Part-Time";

                var facultySchedules = _context.Schedules
                    .Where(s => s.FacultyId == faculty.FacultyId && s.AcademicYear == currentAY)
                    .AsNoTracking()
                    .ToList();

                int assignedUnits = facultySchedules.Sum(s => s.AssignedUnits);

                if (isFullTime && assignedUnits < 18)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        faculty.FacultyId,
                        $"Engr. {faculty.FirstName} {faculty.LastName}",
                        currentAY,
                        assignedUnits,
                        18,
                        $"Full-Time faculty is underloaded with {assignedUnits} units (Minimum required: 18 units).",
                        "Underload"
                    ));
                }
                else if (assignedUnits > facultyMaxUnits)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        faculty.FacultyId,
                        $"Engr. {faculty.FirstName} {faculty.LastName}",
                        currentAY,
                        assignedUnits,
                        facultyMaxUnits,
                        $"{empType} faculty is overloaded with {assignedUnits} units (Maximum allowed: {facultyMaxUnits} units).",
                        "Overload"
                    ));
                }
            }

            AvailableAcademicYears = new List<string>();
            for (int year = 2023; year <= currentYear + 1; year++)
            {
                AvailableAcademicYears.Add($"{year}-{year + 1}");
            }

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
                RoomId = s.RoomId,
                AssignedUnits = s.AssignedUnits
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
            public int AssignedUnits { get; set; }
        }
    }
}
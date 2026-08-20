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
        [BindProperty] public string ScheduleTimeMode { get; set; } = "common";
        [BindProperty] public Dictionary<string, string> DayStartTimes { get; set; } = new();
        [BindProperty] public Dictionary<string, string> DayEndTimes { get; set; } = new();

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
        [BindProperty] public string EditScheduleTimeMode { get; set; } = "common";
        [BindProperty] public Dictionary<string, string> EditDayStartTimes { get; set; } = new();
        [BindProperty] public Dictionary<string, string> EditDayEndTimes { get; set; } = new();

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

        private string? GetScheduleConflictMessage(
            int facultyId,
            int subjectId,
            int? roomId,
            List<string> days,
            TimeSpan startTime,
            TimeSpan endTime,
            string? academicYear,
            string? semester,
            int? excludeScheduleId = null)
        {
            var existingSchedules = _context.Schedules
                .AsNoTracking()
                .Include(s => s.Faculty)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Where(s =>
                    s.AcademicYear == academicYear &&
                    s.Semester == semester &&
                    (!excludeScheduleId.HasValue ||
                    s.ScheduleId != excludeScheduleId.Value))
                .ToList();

            foreach (var existing in existingSchedules)
            {
                // Ignore schedules that do not overlap in BOTH day and time.
                if (!CheckOverlap(
                        existing.TimeSlot,
                        days,
                        startTime,
                        endTime))
                {
                    continue;
                }

                bool sameFaculty = existing.FacultyId == facultyId;
                bool sameSubject = existing.SubjectId == subjectId;

                // Nullable comparison is intentional for exact duplicate detection.
                bool sameRoomAssignment = existing.RoomId == roomId;

                // Room conflicts only apply when an actual room is selected.
                bool sameOccupiedRoom =
                    roomId.HasValue &&
                    existing.RoomId.HasValue &&
                    existing.RoomId.Value == roomId.Value;

                string facultyName =
                    $"Engr. {existing.Faculty.FirstName} {existing.Faculty.LastName}";

                string subjectCode = existing.Subject.Code;

                string roomName =
                    existing.Room != null
                        ? existing.Room.Name
                        : "No room";

                // 1. Same faculty + same subject + overlapping schedule.
                if (sameFaculty && sameSubject)
                {
                    return
                        $"Duplicate schedule detected. {facultyName} already has " +
                        $"{subjectCode} scheduled at {existing.TimeSlot}.";
                }

                // 2. Same faculty AND same room are both occupied.
                if (sameFaculty && sameOccupiedRoom)
                {
                    return
                        $"Schedule conflict detected. {facultyName} is already " +
                        $"scheduled at {existing.TimeSlot}, and room {roomName} " +
                        $"is also occupied during that time.";
                }

                // 3. Faculty cannot teach two subjects at the same time.
                if (sameFaculty)
                {
                    return
                        $"Faculty schedule conflict. {facultyName} is already " +
                        $"assigned to {subjectCode} at {existing.TimeSlot}.";
                }

                // 4. Room cannot be used by two schedules at the same time.
                if (sameOccupiedRoom)
                {
                    return
                        $"Room schedule conflict. Room {roomName} is already " +
                        $"being used for {subjectCode} at {existing.TimeSlot}.";
                }
            }

            return null;
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

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == SelectedFacultyId
                            && s.AcademicYear == SelectedAcademicYear)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.FirstOrDefault(u => u.FacultyId == SelectedFacultyId);

            // Automatically determine employment type & policy limit
            bool isFullTime = (faculty?.MaxUnits ?? 18) >= 18;
            int policyMaxUnits = isFullTime ? 30 : 17;
            string empType = isFullTime ? "Full-Time" : "Part-Time";

            // Save policy update to DB if needed
            if (faculty != null && faculty.MaxUnits != policyMaxUnits)
            {
                faculty.MaxUnits = policyMaxUnits;
                _context.SaveChanges();
            }

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > policyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({policyMaxUnits} units) for {empType} faculty Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Added course: {officialSubjectUnits} units.";
                LoadPageData();
                return Page();
            }

            int? selectedRoomId = SelectedRoomId > 0 ? SelectedRoomId : null;

            var newSchedulesToSave = new List<Schedule>();

            if (ScheduleTimeMode == "custom")
            {
                bool isFirstDay = true;
                foreach (var day in SelectedDays)
                {
                    if (!DayStartTimes.TryGetValue(day, out var startStr) || !DayEndTimes.TryGetValue(day, out var endStr) ||
                        !TimeSpan.TryParse(startStr, out TimeSpan start) || !TimeSpan.TryParse(endStr, out TimeSpan end))
                    {
                        TempData["ErrorMessage"] = $"Invalid time range for {day}.";
                        LoadPageData();
                        return Page();
                    }

                    if (end <= start)
                    {
                        TempData["ErrorMessage"] = $"Invalid time range for {day}.";
                        LoadPageData();
                        return Page();
                    }

                    // --- DUPLICATE / CONFLICT VALIDATION ---
                    var conflictMessage = GetScheduleConflictMessage(
                        SelectedFacultyId,
                        SelectedSubjectId,
                        selectedRoomId,
                        new List<string> { day },
                        start,
                        end,
                        SelectedAcademicYear,
                        SelectedSemester
                    );

                    if (conflictMessage != null)
                    {
                        TempData["ErrorMessage"] = conflictMessage;
                        LoadPageData();
                        return Page();
                    }

                    string formattedTimeSlot =
                        $"{day} " +
                        $"{DateTime.Today.Add(start):h:mm tt} - " +
                        $"{DateTime.Today.Add(end):h:mm tt}";

                    int assignedUnits = isFirstDay ? officialSubjectUnits : 0;
                    isFirstDay = false;

                    newSchedulesToSave.Add(new Schedule
                    {
                        FacultyId = SelectedFacultyId,
                        SubjectId = SelectedSubjectId,
                        RoomId = selectedRoomId,
                        TimeSlot = formattedTimeSlot,
                        AssignedUnits = assignedUnits,
                        OfferCode = SelectedOfferCode,
                        AcademicYear = SelectedAcademicYear,
                        Semester = SelectedSemester
                    });
                }
            }
            else
            {
                if (EndTime <= StartTime)
                {
                    TempData["ErrorMessage"] = "Invalid time range.";
                    LoadPageData();
                    return Page();
                }

                // --- DUPLICATE / CONFLICT VALIDATION ---
                var conflictMessage = GetScheduleConflictMessage(
                    SelectedFacultyId,
                    SelectedSubjectId,
                    selectedRoomId,
                    SelectedDays,
                    StartTime,
                    EndTime,
                    SelectedAcademicYear,
                    SelectedSemester
                );

                if (conflictMessage != null)
                {
                    TempData["ErrorMessage"] = conflictMessage;
                    LoadPageData();
                    return Page();
                }

                string joinedDays = string.Join(", ", SelectedDays);

                string formattedTimeSlot =
                    $"{joinedDays} " +
                    $"{DateTime.Today.Add(StartTime):h:mm tt} - " +
                    $"{DateTime.Today.Add(EndTime):h:mm tt}";

                newSchedulesToSave.Add(new Schedule
                {
                    FacultyId = SelectedFacultyId,
                    SubjectId = SelectedSubjectId,
                    RoomId = selectedRoomId,
                    TimeSlot = formattedTimeSlot,
                    AssignedUnits = officialSubjectUnits,
                    OfferCode = SelectedOfferCode,
                    AcademicYear = SelectedAcademicYear,
                    Semester = SelectedSemester
                });
            }

            foreach (var s in newSchedulesToSave)
            {
                _context.Schedules.Add(s);
            }
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

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == EditSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == EditFacultyId
                            && s.AcademicYear == EditAcademicYear
                            && s.ScheduleId != EditScheduleId)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.FirstOrDefault(u => u.FacultyId == EditFacultyId);
            bool isFullTime = (faculty?.MaxUnits ?? 18) >= 18;
            int policyMaxUnits = isFullTime ? 30 : 17;
            string empType = isFullTime ? "Full-Time" : "Part-Time";

            if (faculty != null && faculty.MaxUnits != policyMaxUnits)
            {
                faculty.MaxUnits = policyMaxUnits;
                _context.SaveChanges();
            }

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > policyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({policyMaxUnits} units) for {empType} faculty Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Course: {officialSubjectUnits} units.";
                return RedirectToPage();
            }

            int? editRoomId =
                EditRoomId > 0 ? EditRoomId : null;

            var newSchedulesToInsert = new List<Schedule>();

            if (EditScheduleTimeMode == "custom")
            {
                bool isFirstDay = true;
                foreach (var day in EditDays)
                {
                    if (!EditDayStartTimes.TryGetValue(day, out var startStr) || !EditDayEndTimes.TryGetValue(day, out var endStr) ||
                        !TimeSpan.TryParse(startStr, out TimeSpan start) || !TimeSpan.TryParse(endStr, out TimeSpan end))
                    {
                        TempData["ErrorMessage"] = $"Invalid time range for {day}.";
                        return RedirectToPage();
                    }

                    if (end <= start)
                    {
                        TempData["ErrorMessage"] = $"Invalid time range for {day}.";
                        return RedirectToPage();
                    }

                    // --- DUPLICATE / CONFLICT VALIDATION ---
                    var conflictMessage = GetScheduleConflictMessage(
                        EditFacultyId,
                        EditSubjectId,
                        editRoomId,
                        new List<string> { day },
                        start,
                        end,
                        EditAcademicYear,
                        EditSemester,
                        isFirstDay ? EditScheduleId : null
                    );

                    if (conflictMessage != null)
                    {
                        TempData["ErrorMessage"] = conflictMessage;
                        return RedirectToPage();
                    }

                    string formattedTimeSlot =
                        $"{day} " +
                        $"{DateTime.Today.Add(start):h:mm tt} - " +
                        $"{DateTime.Today.Add(end):h:mm tt}";

                    int assignedUnits = isFirstDay ? officialSubjectUnits : 0;

                    if (isFirstDay)
                    {
                        schedule.FacultyId = EditFacultyId;
                        schedule.SubjectId = EditSubjectId;
                        schedule.RoomId = editRoomId;
                        schedule.AssignedUnits = assignedUnits;
                        schedule.TimeSlot = formattedTimeSlot;
                        schedule.OfferCode = EditOfferCode;
                        schedule.AcademicYear = EditAcademicYear;
                        schedule.Semester = EditSemester;
                        isFirstDay = false;
                    }
                    else
                    {
                        newSchedulesToInsert.Add(new Schedule
                        {
                            FacultyId = EditFacultyId,
                            SubjectId = EditSubjectId,
                            RoomId = editRoomId,
                            TimeSlot = formattedTimeSlot,
                            AssignedUnits = assignedUnits,
                            OfferCode = EditOfferCode,
                            AcademicYear = EditAcademicYear,
                            Semester = EditSemester
                        });
                    }
                }
            }
            else
            {
                if (EditEndTime <= EditStartTime)
                {
                    TempData["ErrorMessage"] = "Invalid time range.";
                    return RedirectToPage();
                }

                // --- DUPLICATE / CONFLICT VALIDATION ---
                var conflictMessage = GetScheduleConflictMessage(
                    EditFacultyId,
                    EditSubjectId,
                    editRoomId,
                    EditDays,
                    EditStartTime,
                    EditEndTime,
                    EditAcademicYear,
                    EditSemester,
                    EditScheduleId
                );

                if (conflictMessage != null)
                {
                    TempData["ErrorMessage"] = conflictMessage;
                    return RedirectToPage();
                }

                schedule.FacultyId = EditFacultyId;
                schedule.SubjectId = EditSubjectId;
                schedule.RoomId = editRoomId;
                schedule.AssignedUnits = officialSubjectUnits;

                string joinedEditDays = string.Join(", ", EditDays);
                schedule.TimeSlot = $"{joinedEditDays} {DateTime.Today.Add(EditStartTime):h:mm tt} - {DateTime.Today.Add(EditEndTime):h:mm tt}";

                schedule.OfferCode = EditOfferCode;
                schedule.AcademicYear = EditAcademicYear;
                schedule.Semester = EditSemester;
            }

            foreach (var newSched in newSchedulesToInsert)
            {
                _context.Schedules.Add(newSched);
            }

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

            var activeUserString = HttpContext.Session.GetString("ActiveUser");

        if (string.IsNullOrEmpty(activeUserString) ||
            !int.TryParse(activeUserString, out int activeFacultyId))
        {
            ActiveFaculty = new List<User>();
        }
        else
        {
            var chairman = _context.Users
                .FirstOrDefault(u =>
                    u.FacultyId == activeFacultyId &&
                    u.Role == "Chairman");

            if (chairman == null || chairman.DepartmentId == null)
            {
                ActiveFaculty = new List<User>();
            }
            else
            {
                int chairmanDepartmentId = chairman.DepartmentId.Value;

                ActiveFaculty = _context.Users
                    .Where(u =>
                        u.Role == "Faculty" &&
                        u.DepartmentId == chairmanDepartmentId &&
                        !u.Is_delete &&
                        (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                    .ToList();
            }
        }
            ActiveSubjects = _context.Subjects.Where(s => !s.Is_delete).ToList();
            AllRooms = _context.Rooms.ToList();

            // Calculate current Academic Year dynamically
            int currentYear = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string currentAY = (month >= 1 && month <= 5) ? $"{currentYear - 1}-{currentYear}" : $"{currentYear}-{currentYear + 1}";
            SelectedAcademicYear = currentAY;

            // Automatically update DB values for active faculty
            bool changesMade = false;
            foreach (var faculty in ActiveFaculty)
            {
                bool isFT = faculty.MaxUnits >= 18;
                int targetMax = isFT ? 30 : 17;
                if (faculty.MaxUnits != targetMax)
                {
                    faculty.MaxUnits = targetMax;
                    changesMade = true;
                }
            }
            if (changesMade)
            {
                _context.SaveChanges();
            }

            var assignedUnitsByFaculty = _context.Schedules
                .AsNoTracking()
                .Where(s => s.AcademicYear == currentAY)
                .GroupBy(s => s.FacultyId)
                .Select(group => new
                {
                    FacultyId = group.Key,
                    TotalAssignedUnits = group.Sum(s => s.AssignedUnits)
                })
                .ToDictionary(item => item.FacultyId, item => item.TotalAssignedUnits);

            FacultyProfilesLoadList = ActiveFaculty.Select(f =>
            {
                assignedUnitsByFaculty.TryGetValue(f.FacultyId, out int totalUnits);

                int facultyMaxUnits = f.MaxUnits;
                bool isFullTime = facultyMaxUnits >= 18;
                string employmentType = isFullTime ? "Full-Time" : "Part-Time";

                return new FacultyLoadProfileView
                {
                    FacultyId = f.FacultyId,
                    FirstName = f.FirstName,
                    LastName = f.LastName,
                    Email = f.Email,
                    EmploymentType = employmentType,
                    MaxUnits = facultyMaxUnits,
                    TotalAssignedUnits = totalUnits,
                    IsUnderloaded = isFullTime && totalUnits < 18,
                    IsOverloaded = totalUnits > facultyMaxUnits
                };
            }).ToList();
            // --- CALCULATE ALL FACULTY ALERTS ---
            FacultyAlerts = new List<FacultyAlertView>();

            foreach (var facultyProfile in FacultyProfilesLoadList)
            {
                string facultyName = $"Engr. {facultyProfile.FirstName} {facultyProfile.LastName}";

                if (facultyProfile.IsUnderloaded)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        facultyProfile.FacultyId,
                        facultyName,
                        currentAY,
                        facultyProfile.TotalAssignedUnits,
                        18,
                        $"Full-Time faculty is underloaded with {facultyProfile.TotalAssignedUnits} units (Minimum required: 18 units).",
                        "Underload"
                    ));
                }
                else if (facultyProfile.IsOverloaded)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        facultyProfile.FacultyId,
                        facultyName,
                        currentAY,
                        facultyProfile.TotalAssignedUnits,
                        facultyProfile.MaxUnits,
                        $"{facultyProfile.EmploymentType} faculty is overloaded with {facultyProfile.TotalAssignedUnits} units (Maximum allowed: {facultyProfile.MaxUnits} units).",
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
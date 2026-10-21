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

            if (HttpContext.Session.GetString("ShowFacultyLoadPopup") == "true")
            {
                ShowFacultyLoadPopup = true;
                HttpContext.Session.Remove("ShowFacultyLoadPopup");
            }
            else
            {
                ShowFacultyLoadPopup = false;
            }

            return Page();
        }

        public IActionResult OnGetAvailableRooms(string days, string startTime, string endTime, string? academicYear, string? semester, int? excludeScheduleId)
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
            var schedulesQuery = _context.Schedules.AsQueryable();

            if (!string.IsNullOrEmpty(academicYear))
            {
                schedulesQuery = schedulesQuery.Where(s => s.AcademicYear == academicYear);
            }

            if (!string.IsNullOrEmpty(semester))
            {
                schedulesQuery = schedulesQuery.Where(s => s.Semester == semester);
            }

            var activeSchedules = schedulesQuery.ToList();

            var occupiedRoomIds = activeSchedules
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

        private static HashSet<string> ExtractDayTokens(string dayStr)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(dayStr)) return tokens;

            var rawTokens = dayStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in rawTokens)
            {
                var trimmed = raw.Trim();
                int i = 0;
                while (i < trimmed.Length)
                {
                    if (i + 1 < trimmed.Length && trimmed.Substring(i, 2).Equals("Th", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("Th");
                        i += 2;
                    }
                    else if (trimmed.Substring(i).StartsWith("Mon", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("M");
                        i += 3;
                    }
                    else if (trimmed.Substring(i).StartsWith("Tue", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("T");
                        i += 3;
                    }
                    else if (trimmed.Substring(i).StartsWith("Wed", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("W");
                        i += 3;
                    }
                    else if (trimmed.Substring(i).StartsWith("Thu", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("Th");
                        i += 3;
                    }
                    else if (trimmed.Substring(i).StartsWith("Fri", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("F");
                        i += 3;
                    }
                    else if (trimmed.Substring(i).StartsWith("Sat", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add("S");
                        i += 3;
                    }
                    else
                    {
                        char c = char.ToUpperInvariant(trimmed[i]);
                        if (c == 'M' || c == 'T' || c == 'W' || c == 'F' || c == 'S')
                        {
                            tokens.Add(c.ToString());
                        }
                        i++;
                    }
                }
            }
            return tokens;
        }

        private bool CheckOverlap(string timeSlot1, List<string> days2, TimeSpan start2, TimeSpan end2)
        {
            if (string.IsNullOrWhiteSpace(timeSlot1) || days2 == null || days2.Count == 0) return false;

            var match = System.Text.RegularExpressions.Regex.Match(
                timeSlot1.Trim(),
                @"^(?<days>.+?)\s+(?<time>\d{1,2}:\d{2}\s*(?:AM|PM|am|pm)\s*-\s*\d{1,2}:\d{2}\s*(?:AM|PM|am|pm))$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (!match.Success) return false;

            var days1Tokens = ExtractDayTokens(match.Groups["days"].Value);
            var days2Tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in days2)
            {
                foreach (var token in ExtractDayTokens(d))
                {
                    days2Tokens.Add(token);
                }
            }

            bool daysOverlap = days1Tokens.Overlaps(days2Tokens);
            if (!daysOverlap) return false;

            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                match.Groups["time"].Value,
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

            if (SelectedFacultyId == 0 || SelectedDays == null || SelectedDays.Count == 0 || SelectedSubjectId == 0 || string.IsNullOrEmpty(SelectedSemester) || string.IsNullOrEmpty(SelectedAcademicYear))
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields, including days, semester, and academic year.";
                LoadPageData();
                return Page();
            }

            var targetSubject = _context.Subjects.Include(s => s.Department).FirstOrDefault(s => s.SubjectId == SelectedSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == SelectedFacultyId
                            && s.AcademicYear == SelectedAcademicYear
                            && s.Semester == SelectedSemester)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.Include(u => u.Department).FirstOrDefault(u => u.FacultyId == SelectedFacultyId);

            int? chairmanDeptId = null;
            if (int.TryParse(activeUserString, out int activeFacultyId))
            {
                var chairman = _context.Users.Include(u => u.Department).FirstOrDefault(u => u.FacultyId == activeFacultyId && u.Role == "Chairman");
                chairmanDeptId = chairman?.DepartmentId;
            }

            bool isChairmanCre = IsCreDepartment(chairmanDeptId);
            bool isFacultyCre = faculty != null && (IsCreDepartment(faculty.Department) || IsCreDepartment(faculty.DepartmentId));

            if (isChairmanCre || isFacultyCre)
            {
                if (!IsReedSubject(targetSubject))
                {
                    TempData["ErrorMessage"] = "Only REED subjects can be assigned to faculty under the Center for Religious Education (CRE).";
                    LoadPageData();
                    return Page();
                }
            }

            // Automatically determine employment type & policy limit
            bool isFullTime = (faculty?.FacultyType?.Equals("Full-Time", StringComparison.OrdinalIgnoreCase) == true) || ((faculty?.MaxUnits ?? 18) >= 18);
            var settings = _context.SystemSettings.FirstOrDefault();
            int maxOverload = settings?.MaxOverload ?? 30;
            int policyMaxUnits = isFullTime ? maxOverload : 17;
            string empType = isFullTime ? "Full-Time" : "Part-Time";

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > policyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({policyMaxUnits} units) for {empType} faculty Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Added course: {officialSubjectUnits} units.";
                LoadPageData();
                return Page();
            }

            // --- OFFER CODE UNIQUENESS VALIDATION ---
            if (SelectedOfferCode.HasValue && SelectedOfferCode.Value > 0)
            {
                var existingOffer = _context.Schedules
                    .Include(s => s.Subject)
                    .Include(s => s.Faculty)
                    .FirstOrDefault(s => s.OfferCode == SelectedOfferCode
                                      && s.AcademicYear == SelectedAcademicYear
                                      && s.Semester == SelectedSemester);

                if (existingOffer != null)
                {
                    TempData["ErrorMessage"] = $"Offer code '{SelectedOfferCode}' is already in use by {existingOffer.Subject?.Code} ({existingOffer.Subject?.Title}) assigned to Engr. {existingOffer.Faculty?.FirstName} {existingOffer.Faculty?.LastName} for {SelectedSemester} A.Y. {SelectedAcademicYear}. Every scheduled class must have a unique offer code.";
                    LoadPageData();
                    return Page();
                }
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
            var activeUserString = HttpContext.Session.GetString("ActiveUser");
            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == EditScheduleId);
            if (schedule == null) return RedirectToPage();
            int? oldOfferCode = schedule.OfferCode;

            if (EditDays == null || EditDays.Count == 0)
            {
                TempData["ErrorMessage"] = "You must select at least one day.";
                return RedirectToPage();
            }

            var targetSubject = _context.Subjects.Include(s => s.Department).FirstOrDefault(s => s.SubjectId == EditSubjectId);
            int officialSubjectUnits = targetSubject?.Units ?? 0;

            int currentTotalUnits = _context.Schedules
                .Where(s => s.FacultyId == EditFacultyId
                            && s.AcademicYear == EditAcademicYear
                            && s.Semester == EditSemester
                            && s.ScheduleId != EditScheduleId)
                .Sum(s => s.AssignedUnits);

            var faculty = _context.Users.Include(u => u.Department).FirstOrDefault(u => u.FacultyId == EditFacultyId);

            int? chairmanDeptId = null;
            if (int.TryParse(activeUserString, out int activeFacultyId))
            {
                var chairman = _context.Users.Include(u => u.Department).FirstOrDefault(u => u.FacultyId == activeFacultyId && u.Role == "Chairman");
                chairmanDeptId = chairman?.DepartmentId;
            }

            bool isChairmanCre = IsCreDepartment(chairmanDeptId);
            bool isFacultyCre = faculty != null && (IsCreDepartment(faculty.Department) || IsCreDepartment(faculty.DepartmentId));

            if (isChairmanCre || isFacultyCre)
            {
                if (!IsReedSubject(targetSubject))
                {
                    TempData["ErrorMessage"] = "Only REED subjects can be assigned to faculty under the Center for Religious Education (CRE).";
                    LoadPageData();
                    return Page();
                }
            }
            bool isFullTime = (faculty?.FacultyType?.Equals("Full-Time", StringComparison.OrdinalIgnoreCase) == true) || ((faculty?.MaxUnits ?? 18) >= 18);
            var settings = _context.SystemSettings.FirstOrDefault();
            int maxOverload = settings?.MaxOverload ?? 30;
            int policyMaxUnits = isFullTime ? maxOverload : 17;
            string empType = isFullTime ? "Full-Time" : "Part-Time";

            int newTotalUnits = currentTotalUnits + officialSubjectUnits;

            if (newTotalUnits > policyMaxUnits)
            {
                TempData["ErrorMessage"] = $"Assignment exceeds maximum allowed limit ({policyMaxUnits} units) for {empType} faculty Engr. {faculty?.FirstName} {faculty?.LastName}. Current: {currentTotalUnits} units. Course: {officialSubjectUnits} units.";
                return RedirectToPage();
            }

            // --- OFFER CODE UNIQUENESS VALIDATION ---
            if (EditOfferCode.HasValue && EditOfferCode.Value > 0)
            {
                var existingOffer = _context.Schedules
                    .Include(s => s.Subject)
                    .Include(s => s.Faculty)
                    .FirstOrDefault(s => s.OfferCode == EditOfferCode
                                      && s.AcademicYear == EditAcademicYear
                                      && s.Semester == EditSemester
                                      && s.ScheduleId != EditScheduleId
                                      && !(schedule.OfferCode.HasValue
                                           && s.OfferCode == schedule.OfferCode
                                           && s.SubjectId == schedule.SubjectId
                                           && s.FacultyId == schedule.FacultyId));

                if (existingOffer != null)
                {
                    TempData["ErrorMessage"] = $"Offer code '{EditOfferCode}' is already in use by {existingOffer.Subject?.Code} ({existingOffer.Subject?.Title}) assigned to Engr. {existingOffer.Faculty?.FirstName} {existingOffer.Faculty?.LastName} for {EditSemester} A.Y. {EditAcademicYear}. Every scheduled class must have a unique offer code.";
                    return RedirectToPage();
                }
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

            if (oldOfferCode.HasValue && EditOfferCode != oldOfferCode)
            {
                var siblings = _context.Schedules
                    .Where(s => s.OfferCode == oldOfferCode
                             && s.FacultyId == schedule.FacultyId
                             && s.SubjectId == schedule.SubjectId
                             && s.AcademicYear == schedule.AcademicYear
                             && s.Semester == schedule.Semester
                             && s.ScheduleId != EditScheduleId)
                    .ToList();

                foreach (var sib in siblings)
                {
                    sib.OfferCode = EditOfferCode;
                }
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
                TempData["SuccessMessage"] = "Schedule successfully deleted!";
            }
            return RedirectToPage();
        }

        public bool IsCreDepartment(int? departmentId)
        {
            if (!departmentId.HasValue) return false;
            if (departmentId.Value == 83) return true;

            var dept = _context.Departments.FirstOrDefault(d => d.Id == departmentId.Value);
            return IsCreDepartment(dept);
        }

        public bool IsCreDepartment(Department? dept)
        {
            if (dept == null) return false;
            if (dept.Id == 83) return true;

            if (!string.IsNullOrWhiteSpace(dept.Code))
            {
                var code = dept.Code.Trim().ToUpper();
                if (code == "CRE" || code.StartsWith("CRE")) return true;
            }

            if (!string.IsNullOrWhiteSpace(dept.Name))
            {
                var name = dept.Name.Trim().ToUpper();
                if (name.Contains("RELIGIOUS EDUCATION") || name.Contains("CENTER FOR RELIGIOUS EDUCATION")) return true;
                if (name == "CRE" || name.StartsWith("CRE ") || name.EndsWith(" CRE") || name.Contains(" CRE ")) return true;
            }

            return false;
        }

        public bool IsReedSubject(Subject? subject)
        {
            if (subject == null) return false;

            // 1. Check if the subject belongs to CRE department
            if (subject.DepartmentId.HasValue && IsCreDepartment(subject.DepartmentId.Value)) return true;
            if (subject.Department != null && IsCreDepartment(subject.Department)) return true;

            // 2. Check if the subject code starts with REED (e.g. "ReEd 1", "REED 2", "RE-ED 3", "REED101")
            var code = (subject.Code ?? "").Trim().ToUpper();
            var normalizedCode = code.Replace(" ", "").Replace("-", "").Replace("_", "");
            if (normalizedCode.StartsWith("REED")) return true;

            // 3. Fallback check for title containing "Religious Education" or "Catholic Theology"
            var title = (subject.Title ?? "").Trim().ToUpper();
            if (title.Contains("RELIGIOUS EDUCATION") || title.Contains("CATHOLIC THEOLOGY")) return true;

            return false;
        }

        private void LoadPageData()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 30;

            int? chairmanDeptId = null;
            var activeUserString = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeUserString) ||
                !int.TryParse(activeUserString, out int activeFacultyId))
            {
                ActiveFaculty = new List<User>();
            }
            else
            {
                var chairman = _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefault(u =>
                        u.FacultyId == activeFacultyId &&
                        u.Role == "Chairman");

                if (chairman == null || chairman.DepartmentId == null)
                {
                    ActiveFaculty = new List<User>();
                }
                else
                {
                    chairmanDeptId = chairman.DepartmentId;
                    int chairmanDepartmentId = chairman.DepartmentId.Value;

                    ActiveFaculty = _context.Users
                        .Include(u => u.Department)
                        .Where(u =>
                            (u.Role == "Faculty" || u.Role == "Chairman") &&
                            u.DepartmentId == chairmanDepartmentId &&
                            !u.Is_delete &&
                            (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                        .ToList();
                }
            }
            
            bool isChairmanCre = IsCreDepartment(chairmanDeptId);
            bool hasCreFaculty = ActiveFaculty.Any(f => IsCreDepartment(f.Department) || IsCreDepartment(f.DepartmentId));

            var subjectsQuery = _context.Subjects
                .Include(s => s.Department)
                .Where(s => !s.Is_delete);

            if (isChairmanCre || hasCreFaculty)
            {
                ActiveSubjects = subjectsQuery
                    .ToList()
                    .Where(s => IsReedSubject(s))
                    .OrderBy(s => s.Code)
                    .ToList();
            }
            else
            {
                ActiveSubjects = subjectsQuery
                    .OrderBy(s => s.Code)
                    .ToList();
            }


            AllRooms = _context.Rooms.ToList();

            // Calculate current Academic Year dynamically
            int currentYear = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string currentAY = (month >= 1 && month <= 5) ? $"{currentYear - 1}-{currentYear}" : $"{currentYear}-{currentYear + 1}";
            if (string.IsNullOrEmpty(SelectedAcademicYear))
            {
                SelectedAcademicYear = currentAY;
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

                bool isFullTime = (f.FacultyType?.Equals("Full-Time", StringComparison.OrdinalIgnoreCase) == true) || (f.MaxUnits >= 18);
                int facultyMaxUnits = isFullTime ? GlobalMaxLimit : 17;
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

            var schedulesQuery = _context.Schedules.AsQueryable();
            if (chairmanDeptId != null)
            {
                schedulesQuery = schedulesQuery.Where(s => s.Faculty.DepartmentId == chairmanDeptId.Value);
            }
            else
            {
                schedulesQuery = schedulesQuery.Where(s => false);
            }

            CurrentSchedules = schedulesQuery
                .OrderByDescending(s => s.AcademicYear)
                .ThenByDescending(s => s.ScheduleId)
                .Select(s => new AssignedLoad
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
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

        // --- ADD FORM INPUTS ---
        [BindProperty] public string SelectedFacultyId { get; set; } = string.Empty;
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }
        [BindProperty] public string SelectedDays { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }

        // --- EDIT FORM INPUTS ---
        [BindProperty] public int EditScheduleId { get; set; }
        [BindProperty] public string EditFacultyId { get; set; } = string.Empty;
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public int EditRoomId { get; set; }
        [BindProperty] public string EditDays { get; set; }
        [BindProperty] public TimeSpan EditStartTime { get; set; }
        [BindProperty] public TimeSpan EditEndTime { get; set; }
        [BindProperty] public int? EditOfferCode { get; set; }
        [BindProperty] public string EditAcademicYear { get; set; }

        // --- DELETE FORM INPUT ---
        [BindProperty] public int DeleteScheduleId { get; set; }

        [BindProperty] public int GlobalMaxLimit { get; set; }

        // --- LIST TO DISPLAY IN THE TABLE ---
        public IList<AssignedLoad> CurrentSchedules { get; set; } = new List<AssignedLoad>();

        public IActionResult OnGet()
        {
            var activeUser = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeUser))
            {
                return RedirectToPage("/LoginPage");
            }

            LoadPageData();
            return Page();
        }

        // --- ADD SCHEDULE ---
        public IActionResult OnPost()
        {
            var activeUser = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeUser))
            {
                return RedirectToPage("/LoginPage");
            }

            if (string.IsNullOrWhiteSpace(SelectedFacultyId) || string.IsNullOrEmpty(SelectedDays) || SelectedSubjectId == 0)
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields.";
                LoadPageData();
                return Page();
            }

            if (EndTime <= StartTime)
            {
                TempData["ErrorMessage"] = "Invalid time range: End time must be later than start time.";
                LoadPageData();
                return Page();
            }

            // --- ROOM DOUBLE-BOOKING CHECK ---
            if (SelectedRoomId > 0)
            {
                var existingRoomSchedules = _context.Schedules
                    .Where(s => s.RoomId == SelectedRoomId)
                    .ToList();

                foreach (var existingSchedule in existingRoomSchedules)
                {
                    if (HasScheduleConflict(existingSchedule.TimeSlot, SelectedDays, StartTime, EndTime))
                    {
                        var room = _context.Rooms.FirstOrDefault(r => r.RoomId == SelectedRoomId);

                        TempData["ErrorMessage"] =
                            $"Room Conflict: {room?.Name} is already booked during this time ({existingSchedule.TimeSlot}).";

                        LoadPageData();
                        return Page();
                    }
                }
            }

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

            if (targetSubject == null)
            {
                TempData["ErrorMessage"] = "Selected subject was not found.";
                LoadPageData();
                return Page();
            }

            int officialSubjectUnits = targetSubject.Units;

            var currentSchedules = _context.Schedules
                .Where(s => s.FacultyId == SelectedFacultyId)
                .ToList();

            int currentTotalUnits = currentSchedules.Sum(s => s.AssignedUnits);

            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            // --- FACULTY OVERLOAD CHECK ---
            if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
            {
                TempData["ErrorMessage"] =
                    $"Assignment Blocked: Adding {officialSubjectUnits} units for {targetSubject.Code} pushes this faculty member over the Max Overload limit of {globalMaxLimit} units.";

                LoadPageData();
                return Page();
            }

            string formattedTimeSlot =
                $"{SelectedDays} {DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";

            string autoAssignedSemester = GetDefaultSemesterForSubject(targetSubject.Code);

            var newSchedule = new Schedule
            {
                FacultyId = SelectedFacultyId,
                SubjectId = SelectedSubjectId,
                RoomId = SelectedRoomId > 0 ? SelectedRoomId : null,
                TimeSlot = formattedTimeSlot,
                AssignedUnits = officialSubjectUnits,
                OfferCode = SelectedOfferCode,
                AcademicYear = SelectedAcademicYear,
                Semester = autoAssignedSemester
            };

            _context.Schedules.Add(newSchedule);
            _context.SaveChanges();

            TempData["SuccessMessage"] =
                $"Schedule successfully assigned! Automatically recorded as {autoAssignedSemester}.";

            return RedirectToPage();
        }

        // --- EDIT SCHEDULE ---
        public IActionResult OnPostEditSchedule()
        {
            var activeUser = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeUser))
            {
                return RedirectToPage("/LoginPage");
            }

            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == EditScheduleId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "The selected schedule could not be found.";
                LoadPageData();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(EditFacultyId) || EditSubjectId == 0 || string.IsNullOrEmpty(EditDays))
            {
                TempData["ErrorMessage"] = "Please complete all required fields before updating the schedule.";
                LoadPageData();
                return Page();
            }

            if (EditEndTime <= EditStartTime)
            {
                TempData["ErrorMessage"] = "Invalid time range: End time must be later than start time.";
                LoadPageData();
                return Page();
            }

            var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == EditSubjectId);

            if (targetSubject == null)
            {
                TempData["ErrorMessage"] = "Selected subject was not found.";
                LoadPageData();
                return Page();
            }

            int officialSubjectUnits = targetSubject.Units;

            // --- ROOM CONFLICT CHECK, EXCLUDING CURRENT SCHEDULE ---
            if (EditRoomId > 0)
            {
                var existingRoomSchedules = _context.Schedules
                    .Where(s => s.RoomId == EditRoomId && s.ScheduleId != EditScheduleId)
                    .ToList();

                foreach (var existingSchedule in existingRoomSchedules)
                {
                    if (HasScheduleConflict(existingSchedule.TimeSlot, EditDays, EditStartTime, EditEndTime))
                    {
                        var room = _context.Rooms.FirstOrDefault(r => r.RoomId == EditRoomId);

                        TempData["ErrorMessage"] =
                            $"Room Conflict: {room?.Name} is already booked during this time ({existingSchedule.TimeSlot}).";

                        LoadPageData();
                        return Page();
                    }
                }
            }

            // --- FACULTY OVERLOAD CHECK, EXCLUDING CURRENT SCHEDULE ---
            var otherFacultySchedules = _context.Schedules
                .Where(s => s.FacultyId == EditFacultyId && s.ScheduleId != EditScheduleId)
                .ToList();

            int currentTotalUnitsWithoutThisSchedule = otherFacultySchedules.Sum(s => s.AssignedUnits);

            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            if ((currentTotalUnitsWithoutThisSchedule + officialSubjectUnits) > globalMaxLimit)
            {
                TempData["ErrorMessage"] =
                    $"Update Blocked: Assigning {officialSubjectUnits} units for {targetSubject.Code} pushes this faculty member over the Max Overload limit of {globalMaxLimit} units.";

                LoadPageData();
                return Page();
            }

            string formattedTimeSlot =
                $"{EditDays} {DateTime.Today.Add(EditStartTime):h:mm tt} - {DateTime.Today.Add(EditEndTime):h:mm tt}";

            string autoAssignedSemester = GetDefaultSemesterForSubject(targetSubject.Code);

            schedule.FacultyId = EditFacultyId;
            schedule.SubjectId = EditSubjectId;
            schedule.RoomId = EditRoomId > 0 ? EditRoomId : null;
            schedule.TimeSlot = formattedTimeSlot;
            schedule.AssignedUnits = officialSubjectUnits;
            schedule.OfferCode = EditOfferCode;
            schedule.AcademicYear = EditAcademicYear;
            schedule.Semester = autoAssignedSemester;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Schedule successfully updated.";

            return RedirectToPage();
        }

        // --- DELETE SCHEDULE ---
        public IActionResult OnPostDeleteSchedule()
        {
            var activeUser = HttpContext.Session.GetString("ActiveUser");

            if (string.IsNullOrEmpty(activeUser))
            {
                return RedirectToPage("/LoginPage");
            }

            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == DeleteScheduleId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "The selected schedule could not be found or may have already been deleted.";
                LoadPageData();
                return Page();
            }

            _context.Schedules.Remove(schedule);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Schedule successfully deleted.";

            return RedirectToPage();
        }

        // --- SEMESTER AUTO-MAPPING ---
        private string GetDefaultSemesterForSubject(string courseCode)
        {
            if (string.IsNullOrEmpty(courseCode))
            {
                return "1st Sem";
            }

            return courseCode.ToUpper().Trim() switch
            {
                "CS101" => "1st Sem",
                "CS105" => "1st Sem",
                "PHY101" => "2nd Sem",
                "CS102" => "2nd Sem",
                "CS201" => "3rd Sem",
                "CPE212" => "3rd Sem",
                "CPE221" => "4th Sem",
                "CPE222" => "4th Sem",
                "ENT101" => "1st Sem",
                "CPE311" => "1st Sem",
                "CPE312" => "1st Sem",
                "CPE321" => "2nd Sem",
                "CPE322" => "2nd Sem",
                "CPE626" => "2nd Sem",
                _ => "1st Sem"
            };
        }

        private bool HasScheduleConflict(string existingTimeSlot, string newDays, TimeSpan newStartTime, TimeSpan newEndTime)
        {
            if (string.IsNullOrWhiteSpace(existingTimeSlot) || string.IsNullOrWhiteSpace(newDays))
            {
                return false;
            }

            var parts = existingTimeSlot.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 6)
            {
                return false;
            }

            string existingDays = parts[0];

            bool daysOverlap = DaysOverlap(existingDays, newDays);

            if (!daysOverlap)
            {
                return false;
            }

            try
            {
                string existStartStr = $"{parts[1]} {parts[2]}";
                string existEndStr = $"{parts[4]} {parts[5]}";

                TimeSpan existStart = DateTime.Parse(existStartStr).TimeOfDay;
                TimeSpan existEnd = DateTime.Parse(existEndStr).TimeOfDay;

                return newStartTime < existEnd && newEndTime > existStart;
            }
            catch
            {
                string formattedNewTime =
                    $"{newDays} {DateTime.Today.Add(newStartTime):h:mm tt} - {DateTime.Today.Add(newEndTime):h:mm tt}";

                return existingTimeSlot == formattedNewTime;
            }
        }

        private bool DaysOverlap(string existingDays, string newDays)
        {
            var existingDayList = ExpandDayCodes(existingDays);
            var newDayList = ExpandDayCodes(newDays);

            return existingDayList.Any(day => newDayList.Contains(day));
        }

        private List<string> ExpandDayCodes(string days)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(days))
            {
                return result;
            }

            string code = days.ToUpper().Trim();

            // Supports M, T, W, Th/TH, F, S, MWF, TTh/TTH
            if (code.Contains("M"))
            {
                result.Add("M");
            }

            if (code.Contains("TH"))
            {
                result.Add("TH");
            }

            string codeWithoutThursday = code.Replace("TH", "");

            if (codeWithoutThursday.Contains("T"))
            {
                result.Add("T");
            }

            if (codeWithoutThursday.Contains("W"))
            {
                result.Add("W");
            }

            if (codeWithoutThursday.Contains("F"))
            {
                result.Add("F");
            }

            if (codeWithoutThursday.Contains("S"))
            {
                result.Add("S");
            }

            return result.Distinct().ToList();
        }

        private void LoadPageData()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 21;

            var facultyMembers = _context.Users
                .Where(u => u.Role == "Faculty" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                .ToList();

            var currentUserFacultyId = HttpContext.Session.GetString("ActiveUser");

            if (!string.IsNullOrEmpty(currentUserFacultyId))
            {
                var chairman = _context.Users
                    .FirstOrDefault(u => u.FacultyId == currentUserFacultyId && u.Role == "Chairman");

                if (chairman != null && !facultyMembers.Any(f => f.Id == chairman.Id))
                {
                    facultyMembers.Add(chairman);
                }
            }

            ActiveFaculty = facultyMembers
                .OrderBy(f => f.LastName)
                .ThenBy(f => f.FirstName)
                .ToList();

            ActiveSubjects = _context.Subjects
                .OrderBy(s => s.Code)
                .ToList();

            AllRooms = _context.Rooms
                .OrderBy(r => r.Name)
                .ToList();

            AvailableAcademicYears = _context.Schedules
                .Where(s => s.AcademicYear != null && s.AcademicYear != "")
                .Select(s => s.AcademicYear)
                .Distinct()
                .OrderByDescending(ay => ay)
                .ToList();

            if (!AvailableAcademicYears.Any())
            {
                AvailableAcademicYears = new List<string>
                {
                    "2024-2025",
                    "2025-2026"
                };
            }

            // IMPORTANT:
            // First project database fields only, then call ToList().
            // String formatting such as $"Engr. {FirstName} {LastName}" must happen in memory,
            // not inside the EF SQL query.
            var scheduleRows = (from s in _context.Schedules
                                join u in _context.Users on s.FacultyId equals u.FacultyId
                                join sub in _context.Subjects on s.SubjectId equals sub.SubjectId
                                join room in _context.Rooms on s.RoomId equals (int?)room.RoomId into roomJoin
                                from r in roomJoin.DefaultIfEmpty()
                                select new
                                {
                                    ScheduleId = s.ScheduleId,
                                    FacultyId = u.FacultyId,
                                    SubjectId = sub.SubjectId,
                                    RoomId = s.RoomId,

                                    FirstName = u.FirstName,
                                    LastName = u.LastName,

                                    CourseCode = sub.Code,
                                    SubjectTitle = sub.Title,

                                    TimeSlot = s.TimeSlot,
                                    RoomName = r != null ? r.Name : "No room assigned",

                                    OfferCode = s.OfferCode,
                                    AcademicYear = s.AcademicYear,
                                    Semester = s.Semester
                                })
                                .ToList();

            CurrentSchedules = scheduleRows
                .Select(row => new AssignedLoad
                {
                    ScheduleId = row.ScheduleId,
                    FacultyId = row.FacultyId,
                    SubjectId = row.SubjectId,
                    RoomId = row.RoomId,

                    FacultyName = $"Engr. {row.FirstName} {row.LastName}",
                    CourseCode = row.CourseCode,
                    SubjectTitle = row.SubjectTitle,
                    Schedule = row.TimeSlot,
                    RoomName = row.RoomName,
                    OfferCode = row.OfferCode,
                    AcademicYear = row.AcademicYear,
                    Semester = row.Semester
                })
                .OrderBy(row => row.FacultyName)
                .ThenBy(row => row.CourseCode)
                .ToList();
        }

        public JsonResult OnGetAvailableRooms(string days, string startTime, string endTime, int? excludeScheduleId)
        {
            if (string.IsNullOrEmpty(days) || string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
            {
                var allRoomsFallback = _context.Rooms
                    .OrderBy(r => r.Name)
                    .Select(r => new { roomId = r.RoomId, name = r.Name })
                    .ToList();

                return new JsonResult(allRoomsFallback);
            }

            try
            {
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);

                var allRooms = _context.Rooms
                    .OrderBy(r => r.Name)
                    .ToList();

                var availableRooms = new List<object>();

                foreach (var room in allRooms)
                {
                    var existingRoomSchedules = _context.Schedules
                        .Where(s => s.RoomId == room.RoomId)
                        .ToList();

                    if (excludeScheduleId.HasValue)
                    {
                        existingRoomSchedules = existingRoomSchedules
                            .Where(s => s.ScheduleId != excludeScheduleId.Value)
                            .ToList();
                    }

                    bool hasConflict = false;

                    foreach (var schedule in existingRoomSchedules)
                    {
                        if (HasScheduleConflict(schedule.TimeSlot, days, start, end))
                        {
                            hasConflict = true;
                            break;
                        }
                    }

                    if (!hasConflict)
                    {
                        availableRooms.Add(new
                        {
                            roomId = room.RoomId,
                            name = room.Name
                        });
                    }
                }

                return new JsonResult(availableRooms);
            }
            catch
            {
                return new JsonResult(new List<object>());
            }
        }

        public class AssignedLoad
        {
            public int ScheduleId { get; set; }
            public string? FacultyId { get; set; }
            public int SubjectId { get; set; }
            public int? RoomId { get; set; }

            public string FacultyName { get; set; }
            public string CourseCode { get; set; }
            public string SubjectTitle { get; set; }
            public string Schedule { get; set; }
            public string RoomName { get; set; }
            public int? OfferCode { get; set; }
            public string AcademicYear { get; set; }
            public string Semester { get; set; }
        }
    }
}
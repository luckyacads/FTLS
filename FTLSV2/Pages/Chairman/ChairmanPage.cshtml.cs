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
        public IList<User> ActiveFaculty { get; set; }
        public IList<Subject> ActiveSubjects { get; set; }
        public IList<Room> AllRooms { get; set; }
        public IList<string> AvailableAcademicYears { get; set; }

        // --- FORM INPUTS TO CATCH ---
        [BindProperty] public int SelectedFacultyId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }

        // --- SEPARATE TIME INPUTS ---
        [BindProperty] public string SelectedDays { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }

        // --- OFFER CODE AND ACADEMIC YEAR INPUTS ---
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }

        [BindProperty] public int GlobalMaxLimit { get; set; }

        // --- LIST TO DISPLAY IN THE TABLE ---
        public IList<AssignedLoad> CurrentSchedules { get; set; }

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

        public IActionResult OnPost()
        {
            if (SelectedFacultyId != 0 && !string.IsNullOrEmpty(SelectedDays) && SelectedSubjectId != 0)
            {
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

                            TempData["ErrorMessage"] = $"Room Conflict: {room?.Name} is already booked during this time ({existingSchedule.TimeSlot}).";

                            LoadPageData();
                            return Page();
                        }
                    }
                }

                var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

                if (targetSubject != null)
                {
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
                        TempData["ErrorMessage"] = $"Assignment Blocked: Adding {officialSubjectUnits} units for {targetSubject.Code} pushes this faculty member over the Max Overload limit of {globalMaxLimit} units.";

                        LoadPageData();
                        return Page();
                    }

                    string formattedTimeSlot = $"{SelectedDays} {DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";

                    var newSchedule = new Schedule
                    {
                        FacultyId = SelectedFacultyId,
                        SubjectId = SelectedSubjectId,
                        RoomId = SelectedRoomId > 0 ? SelectedRoomId : null,
                        TimeSlot = formattedTimeSlot,
                        AssignedUnits = officialSubjectUnits,
                        OfferCode = SelectedOfferCode,
                        AcademicYear = SelectedAcademicYear
                    };

                    _context.Schedules.Add(newSchedule);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = $"Schedule successfully assigned! ({officialSubjectUnits} units automatically added to load)";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields.";

                LoadPageData();
                return Page();
            }

            return RedirectToPage();
        }

        private bool HasScheduleConflict(string existingTimeSlot, string newDays, TimeSpan newStartTime, TimeSpan newEndTime)
        {
            if (string.IsNullOrEmpty(existingTimeSlot))
            {
                return false;
            }

            var parts = existingTimeSlot.Split(' ');

            if (parts.Length < 6)
            {
                return false;
            }

            string existingDays = parts[0];

            bool daysOverlap = newDays.Any(day => existingDays.Contains(day));

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

                if (newStartTime < existEnd && newEndTime > existStart)
                {
                    return true;
                }
            }
            catch
            {
                string formattedNewTime = $"{newDays} {DateTime.Today.Add(newStartTime):h:mm tt} - {DateTime.Today.Add(newEndTime):h:mm tt}";

                return existingTimeSlot == formattedNewTime;
            }

            return false;
        }

        private void LoadPageData()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 21;

            // Get all active faculty users
            var facultyMembers = _context.Users
                .Where(u => u.Role == "Faculty" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status)))
                .ToList();

            // Get the current chairman from session and add them to the dropdown
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
                .Where(s => s.AcademicYear != null)
                .Select(s => s.AcademicYear)
                .Distinct()
                .OrderByDescending(ay => ay)
                .ToList();

            CurrentSchedules = (from s in _context.Schedules
                                join u in _context.Users on s.FacultyId equals u.Id
                                join sub in _context.Subjects on s.SubjectId equals sub.SubjectId
                                join r in _context.Rooms on s.RoomId equals r.RoomId
                                select new AssignedLoad
                                {
                                    FacultyName = $"Engr. {u.FirstName} {u.LastName}",
                                    CourseCode = sub.Code,
                                    Schedule = s.TimeSlot,
                                    RoomName = r.Name,
                                    OfferCode = s.OfferCode,
                                    AcademicYear = s.AcademicYear
                                }).ToList();
        }

        // --- AJAX HANDLER FOR DYNAMIC ROOM FILTERING ---
        public JsonResult OnGetAvailableRooms(string days, string startTime, string endTime)
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
                        availableRooms.Add(new { roomId = room.RoomId, name = room.Name });
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
            public string FacultyName { get; set; }
            public string CourseCode { get; set; }
            public string Schedule { get; set; }
            public string RoomName { get; set; }
            public int? OfferCode { get; set; }
            public string AcademicYear { get; set; }
        }
    }
}
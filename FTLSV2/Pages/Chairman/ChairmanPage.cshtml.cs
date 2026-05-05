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
        public IList<User> ActiveTeachers { get; set; }
        public IList<Subject> ActiveSubjects { get; set; }
        public IList<Room> AllRooms { get; set; }
        public IList<string> AvailableAcademicYears { get; set; }

        // --- FORM INPUTS TO CATCH ---
        [BindProperty] public int SelectedFacultyId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }

        // --- NEW SEPARATE TIME INPUTS ---
        [BindProperty] public string SelectedDays { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }

        // --- NEW OFFER CODE AND ACADEMIC YEAR INPUTS ---
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }

        [BindProperty] public int GlobalMaxLimit { get; set; }

        // --- LIST TO DISPLAY IN THE TABLE ---
        public IList<AssignedLoad> CurrentSchedules { get; set; }

        public void OnGet()
        {
            LoadPageData();
        }

        public IActionResult OnPost()
        {
            // Validate against the new inputs
            if (SelectedFacultyId != 0 && !string.IsNullOrEmpty(SelectedDays) && SelectedSubjectId != 0)
            {
                // 1. --- NEW RESTRICTION: ROOM DOUBLE-BOOKING CHECK ---
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

                            // FIX: Load dropdowns and return the Page to keep form data!
                            LoadPageData();
                            return Page();
                        }
                    }
                }

                var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

                if (targetSubject != null)
                {
                    int officialSubjectUnits = targetSubject.Units;

                    var currentSchedules = _context.Schedules.Where(s => s.FacultyId == SelectedFacultyId).ToList();
                    int currentTotalUnits = currentSchedules.Sum(s => s.AssignedUnits);

                    var settings = _context.SystemSettings.FirstOrDefault();
                    int globalMaxLimit = settings?.MaxOverload ?? 21;

                    // 2. --- TEACHER OVERLOAD CHECK ---
                    if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
                    {
                        TempData["ErrorMessage"] = $"Assignment Blocked: Adding {officialSubjectUnits} units for {targetSubject.Code} pushes this teacher over the Max Overload limit of {globalMaxLimit} units.";

                        // FIX: Load dropdowns and return the Page to keep form data!
                        LoadPageData();
                        return Page();
                    }

                    // *** THE MAGIC TRICK ***
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
                // FIX: Load dropdowns and return the Page to keep form data!
                LoadPageData();
                return Page();
            }

            // IF SUCCESSFUL, redirect to clear the form out.
            return RedirectToPage();
        }

        // --- HELPER METHOD TO PARSE STRINGS AND CHECK OVERLAPS ---
        private bool HasScheduleConflict(string existingTimeSlot, string newDays, TimeSpan newStartTime, TimeSpan newEndTime)
        {
            if (string.IsNullOrEmpty(existingTimeSlot)) return false;

            // Example existing format: "MWF 8:00 AM - 9:30 AM"
            var parts = existingTimeSlot.Split(' ');
            if (parts.Length < 6) return false;

            string existingDays = parts[0];

            // 1. Check if the Days overlap (e.g. "MWF" and "TTh" do NOT overlap. "MWF" and "M" DO overlap)
            bool daysOverlap = newDays.Any(day => existingDays.Contains(day));
            if (!daysOverlap) return false;

            try
            {
                // 2. Parse the existing Start and End times
                // parts[1] = "8:00", parts[2] = "AM", parts[4] = "9:30", parts[5] = "AM"
                string existStartStr = $"{parts[1]} {parts[2]}";
                string existEndStr = $"{parts[4]} {parts[5]}";

                TimeSpan existStart = DateTime.Parse(existStartStr).TimeOfDay;
                TimeSpan existEnd = DateTime.Parse(existEndStr).TimeOfDay;

                // 3. Mathematical Overlap Check: (Start A < End B) AND (End A > Start B)
                if (newStartTime < existEnd && newEndTime > existStart)
                {
                    return true; // CONFLICT FOUND!
                }
            }
            catch
            {
                // If parsing fails for some reason, fallback to an exact string match
                string formattedNewTime = $"{newDays} {DateTime.Today.Add(newStartTime):h:mm tt} - {DateTime.Today.Add(newEndTime):h:mm tt}";
                return existingTimeSlot == formattedNewTime;
            }

            return false;
        }

        private void LoadPageData()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 21;

            // Get all active teachers
            var teachers = _context.Users.Where(u => u.Role == "Teacher" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status))).ToList();

            // Get the current chairman from session and add them to the dropdown
            var currentUserFacultyId = HttpContext.Session.GetString("ActiveUser");
            if (!string.IsNullOrEmpty(currentUserFacultyId))
            {
                var chairman = _context.Users.FirstOrDefault(u => u.FacultyId == currentUserFacultyId && u.Role == "Chairman");
                if (chairman != null && !teachers.Any(t => t.Id == chairman.Id))
                {
                    teachers.Add(chairman);
                }
            }

            // Sort the list by name
            ActiveTeachers = teachers.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();
            ActiveSubjects = _context.Subjects.OrderBy(s => s.Code).ToList();
            AllRooms = _context.Rooms.OrderBy(r => r.Name).ToList();

            // Get all available academic years from the database
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

        // --- NEW: AJAX HANDLER FOR DYNAMIC ROOM FILTERING ---
        public JsonResult OnGetAvailableRooms(string days, string startTime, string endTime)
        {
            // If the user hasn't filled out all 3 time fields yet, return all rooms
            if (string.IsNullOrEmpty(days) || string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
            {
                var allRoomsFallback = _context.Rooms.OrderBy(r => r.Name)
                                             .Select(r => new { roomId = r.RoomId, name = r.Name })
                                             .ToList();
                return new JsonResult(allRoomsFallback);
            }

            try
            {
                // Convert the HTML string times into C# TimeSpans
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);

                var allRooms = _context.Rooms.OrderBy(r => r.Name).ToList();
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

                    // Only add if no conflicts were found
                    if (!hasConflict)
                    {
                        availableRooms.Add(new { roomId = room.RoomId, name = room.Name });
                    }
                }

                return new JsonResult(availableRooms);
            }
            catch
            {
                // Failsafe: if parsing breaks, return an empty list
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
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
        [BindProperty] public string SelectedDays { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }
        [BindProperty] public int? SelectedOfferCode { get; set; }
        [BindProperty] public string SelectedAcademicYear { get; set; }

        [BindProperty] public int EditScheduleId { get; set; }
        [BindProperty] public int EditFacultyId { get; set; }
        [BindProperty] public int EditSubjectId { get; set; }
        [BindProperty] public int EditRoomId { get; set; }
        [BindProperty] public string EditDays { get; set; }
        [BindProperty] public TimeSpan EditStartTime { get; set; }
        [BindProperty] public TimeSpan EditEndTime { get; set; }
        [BindProperty] public int? EditOfferCode { get; set; }
        [BindProperty] public string EditAcademicYear { get; set; }

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

        public IActionResult OnPost()
        {
            var activeUserString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(activeUserString)) return RedirectToPage("/LoginPage");

            if (SelectedFacultyId == 0 || string.IsNullOrEmpty(SelectedDays) || SelectedSubjectId == 0)
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields.";
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
                .Where(s => s.FacultyId == SelectedFacultyId)
                .Sum(s => s.AssignedUnits);

            var settings = _context.SystemSettings.FirstOrDefault();
            int globalMaxLimit = settings?.MaxOverload ?? 21;

            if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
            {
                TempData["ErrorMessage"] = "Assignment exceeds Max Overload limit.";
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
                AcademicYear = SelectedAcademicYear,
                Semester = "1st Sem"
            };

            _context.Schedules.Add(newSchedule);
            _context.SaveChanges();

            return RedirectToPage();
        }

        public IActionResult OnPostEditSchedule()
        {
            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == EditScheduleId);
            if (schedule == null) return Page();

            schedule.FacultyId = EditFacultyId;
            schedule.SubjectId = EditSubjectId;
            schedule.RoomId = EditRoomId > 0 ? EditRoomId : null;
            schedule.TimeSlot = $"{EditDays} {DateTime.Today.Add(EditStartTime):h:mm tt} - {DateTime.Today.Add(EditEndTime):h:mm tt}";
            schedule.OfferCode = EditOfferCode;
            schedule.AcademicYear = EditAcademicYear;

            _context.SaveChanges();
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

            ActiveFaculty = _context.Users.Where(u => u.Role == "Faculty").ToList();
            ActiveSubjects = _context.Subjects.ToList();
            AllRooms = _context.Rooms.ToList();

            CurrentSchedules = _context.Schedules.Select(s => new AssignedLoad
            {
                ScheduleId = s.ScheduleId,
                FacultyId = s.FacultyId,
                FacultyName = "Engr. " + s.Faculty.FirstName + " " + s.Faculty.LastName,
                CourseCode = s.Subject.Code,
                SubjectTitle = s.Subject.Title,
                Schedule = s.TimeSlot,
                RoomName = s.Room != null ? s.Room.Name : "No room",
                // Mapped the missing properties here
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

            // Added these to resolve CS1061 errors in your view
            public int? OfferCode { get; set; }
            public string AcademicYear { get; set; }
            public string Semester { get; set; }
            public int SubjectId { get; set; }
            public int? RoomId { get; set; }
        }
    }
}
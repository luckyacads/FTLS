using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
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

        // --- FORM INPUTS TO CATCH ---
        [BindProperty] public int SelectedFacultyId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedRoomId { get; set; }

        // --- NEW SEPARATE TIME INPUTS ---
        [BindProperty] public string SelectedDays { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }

        [BindProperty] public int GlobalMaxLimit { get; set; }

        // --- LIST TO DISPLAY IN THE TABLE ---
        public IList<AssignedLoad> CurrentSchedules { get; set; }

        public void OnGet()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            GlobalMaxLimit = settings?.MaxOverload ?? 21;

            ActiveTeachers = _context.Users.Where(u => u.Role == "Teacher" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status))).ToList();
            ActiveSubjects = _context.Subjects.OrderBy(s => s.Code).ToList();
            AllRooms = _context.Rooms.OrderBy(r => r.Name).ToList();

            CurrentSchedules = (from s in _context.Schedules
                                join u in _context.Users on s.FacultyId equals u.Id
                                join sub in _context.Subjects on s.SubjectId equals sub.SubjectId
                                join r in _context.Rooms on s.RoomId equals r.RoomId
                                select new AssignedLoad
                                {
                                    FacultyName = $"Engr. {u.FirstName} {u.LastName}",
                                    CourseCode = sub.Code,
                                    Schedule = s.TimeSlot,
                                    RoomName = r.Name
                                }).ToList();
        }

        public IActionResult OnPost()
        {
            // Validate against the new inputs
            if (SelectedFacultyId != 0 && !string.IsNullOrEmpty(SelectedDays) && SelectedSubjectId != 0)
            {
                var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

                if (targetSubject != null)
                {
                    int officialSubjectUnits = targetSubject.Units;

                    var currentSchedules = _context.Schedules.Where(s => s.FacultyId == SelectedFacultyId).ToList();
                    int currentTotalUnits = currentSchedules.Sum(s => s.AssignedUnits);

                    var settings = _context.SystemSettings.FirstOrDefault();
                    int globalMaxLimit = settings?.MaxOverload ?? 21;

                    if ((currentTotalUnits + officialSubjectUnits) > globalMaxLimit)
                    {
                        TempData["ErrorMessage"] = $"Assignment Blocked: Adding {officialSubjectUnits} units for {targetSubject.Code} pushes this teacher over the Max Overload limit of {globalMaxLimit} units.";
                        return RedirectToPage();
                    }

                    // *** THE MAGIC TRICK ***
                    // Combine the three inputs into the standard string format your database uses
                    string formattedTimeSlot = $"{SelectedDays} {DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";

                    var newSchedule = new Schedule
                    {
                        FacultyId = SelectedFacultyId,
                        SubjectId = SelectedSubjectId,
                        RoomId = SelectedRoomId,
                        TimeSlot = formattedTimeSlot, // Save the combined string!
                        AssignedUnits = officialSubjectUnits
                    };

                    _context.Schedules.Add(newSchedule);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Schedule successfully assigned! ({officialSubjectUnits} units automatically added to load)";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all the schedule fields.";
            }
            return RedirectToPage();
        }

        public class AssignedLoad
        {
            public string FacultyName { get; set; }
            public string CourseCode { get; set; }
            public string Schedule { get; set; }
            public string RoomName { get; set; }
        }
    }
}
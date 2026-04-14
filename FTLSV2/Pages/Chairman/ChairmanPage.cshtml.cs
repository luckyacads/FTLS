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
        [BindProperty] public string InputTimeSlot { get; set; }
      

        // --- LIST TO DISPLAY IN THE TABLE ---
        public IList<AssignedLoad> CurrentSchedules { get; set; }

        public void OnGet()
        {
            // 1. Load data for dropdowns
            ActiveTeachers = _context.Users.Where(u => u.Role == "Teacher" && (u.Status == "Active" || string.IsNullOrEmpty(u.Status))).ToList();
            ActiveSubjects = _context.Subjects.OrderBy(s => s.Code).ToList();
            AllRooms = _context.Rooms.OrderBy(r => r.Name).ToList();

            // 2. Load existing schedules to display in the table
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
            if (SelectedFacultyId != 0 && !string.IsNullOrEmpty(InputTimeSlot) && SelectedSubjectId != 0)
            {
                // 1. Find the subject the Chairman selected
                var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

                if (targetSubject != null)
                {
                    // 2. Automatically grab the official units for this subject!
                    int officialSubjectUnits = targetSubject.Units;

                    // Calculate current units the teacher already has
                    var currentSchedules = _context.Schedules.Where(s => s.FacultyId == SelectedFacultyId).ToList();
                    int currentTotalUnits = currentSchedules.Sum(s => s.AssignedUnits);

                    // 3. THE GUARDRAIL: Use the official units to check if it exceeds the limit
                    if ((currentTotalUnits + officialSubjectUnits) > FTLSV2.Pages.AdminPageModel.CurrentMaxOverload)
                    {
                        TempData["ErrorMessage"] = $"Assignment Blocked: Adding {officialSubjectUnits} units for {targetSubject.Code} pushes this teacher over the Max Overload limit of {FTLSV2.Pages.AdminPageModel.CurrentMaxOverload} units.";
                        return RedirectToPage();
                    }

                    // 4. If safe, save the schedule and automatically attach the correct units
                    var newSchedule = new Schedule
                    {
                        FacultyId = SelectedFacultyId,
                        SubjectId = SelectedSubjectId,
                        RoomId = SelectedRoomId,
                        TimeSlot = InputTimeSlot,
                        AssignedUnits = officialSubjectUnits // <-- Automatically saving it!
                    };

                    _context.Schedules.Add(newSchedule);

                    var room = _context.Rooms.FirstOrDefault(r => r.RoomId == SelectedRoomId);
                    if (room != null) room.Availability = "Booked";

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = $"Schedule successfully assigned! ({officialSubjectUnits} units automatically added to load)";
                }
            }
            return RedirectToPage();
        }

        // A helper class just to structure the data for our HTML table
        public class AssignedLoad
        {
            public string FacultyName { get; set; }
            public string CourseCode { get; set; }
            public string Schedule { get; set; }
            public string RoomName { get; set; }
        }
    }
}
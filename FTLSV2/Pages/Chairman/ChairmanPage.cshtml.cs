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
            // FIXED: SelectedFacultyId is a number, so we check if it is != 0
            if (SelectedFacultyId != 0 && !string.IsNullOrEmpty(InputTimeSlot))
            {
                // 1. Find the subject they are trying to assign to get its units
                var targetSubject = _context.Subjects.FirstOrDefault(s => s.SubjectId == SelectedSubjectId);

                if (targetSubject != null)
                {
                    // 2. Calculate how many units this teacher CURRENTLY has
                    var currentSchedules = _context.Schedules.Where(s => s.FacultyId == SelectedFacultyId).ToList();
                    int currentTotalUnits = 0;

                    foreach (var sched in currentSchedules)
                    {
                        var sub = _context.Subjects.FirstOrDefault(s => s.SubjectId == sched.SubjectId);
                        if (sub != null) currentTotalUnits += sub.Units;
                    }

                    // 3. THE GUARDRAIL: Check if adding this new class exceeds the Admin's Max Overload limit
                    if ((currentTotalUnits + targetSubject.Units) > FTLSV2.Pages.AdminPageModel.CurrentMaxOverload)
                    {
                        // Too many units! Block the save and send an error.
                        TempData["ErrorMessage"] = $"Assignment Blocked: Adding this class exceeds the Max Overload limit of {FTLSV2.Pages.AdminPageModel.CurrentMaxOverload} units for this teacher.";
                        return RedirectToPage();
                    }

                    // 4. If it's safe, save the schedule normally!
                    var newSchedule = new Schedule
                    {
                        FacultyId = SelectedFacultyId,
                        SubjectId = SelectedSubjectId,
                        RoomId = SelectedRoomId,
                        TimeSlot = InputTimeSlot
                        
                    };

                    _context.Schedules.Add(newSchedule);

                    var room = _context.Rooms.FirstOrDefault(r => r.RoomId == SelectedRoomId);
                    if (room != null) room.Availability = "Booked";

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Schedule successfully assigned!";
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
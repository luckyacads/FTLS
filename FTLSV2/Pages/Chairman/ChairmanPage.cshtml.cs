using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FTLSV2.Data;
using FTLSV2.Models;
using System.Collections.Generic;
using System.Linq;
using System;

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
        [BindProperty] public string SelectedFacultyId { get; set; }
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
                                join u in _context.Users on s.FacultyId equals u.FacultyId
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
            // Save the new schedule to the database!
            if (!string.IsNullOrEmpty(SelectedFacultyId) && !string.IsNullOrEmpty(InputTimeSlot))
            {
                var newSchedule = new Schedule
                {
                    FacultyId = SelectedFacultyId,
                    SubjectId = SelectedSubjectId,
                    RoomId = SelectedRoomId,
                    TimeSlot = InputTimeSlot,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Schedules.Add(newSchedule);

                // Optional: If you want to automatically mark a room as "Booked" when scheduled
                var room = _context.Rooms.FirstOrDefault(r => r.RoomId == SelectedRoomId);
                if (room != null) room.Availability = "Booked";

                _context.SaveChanges();
            }

            return RedirectToPage(); // Refresh the page to show the new data
        }
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
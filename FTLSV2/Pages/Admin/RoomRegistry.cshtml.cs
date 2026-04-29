using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using FTLSV2.Models;

namespace FTLSV2.Pages.Admin
{
    public class RoomRegistryModel : PageModel
    {
        private readonly Data.FtlsDbContext _db;

        public RoomRegistryModel(Data.FtlsDbContext db)
        {
            _db = db;
        }

        public class RoomDisplay
        {
            public int RoomId { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public int Capacity { get; set; }
            public string Availability { get; set; }
        }

        public List<RoomDisplay> Rooms { get; set; } = new List<RoomDisplay>();

        [BindProperty] public string NewRoomName { get; set; }
        [BindProperty] public string NewRoomType { get; set; }
        [BindProperty] public int NewRoomCapacity { get; set; }

        [BindProperty] public int EditRoomId { get; set; }
        [BindProperty] public string EditRoomName { get; set; }
        [BindProperty] public string EditRoomType { get; set; }
        [BindProperty] public int EditRoomCapacity { get; set; }

        public void OnGet()
        {
            var allRooms = _db.Rooms.OrderBy(r => r.Name).ToList();
            var allSchedules = _db.Schedules.ToList();

            foreach (var room in allRooms)
            {
                var roomSchedules = allSchedules.Where(s => s.RoomId == room.RoomId && !string.IsNullOrEmpty(s.TimeSlot)).ToList();

                // 1. Group schedules by MWF and TTh
                var mwfSchedules = roomSchedules.Where(s => s.TimeSlot.StartsWith("MWF")).Select(s => ExtractTimeSpan(s.TimeSlot)).Where(t => t.HasValue).Select(t => t.Value).OrderBy(t => t.Start).ToList();
                var tthSchedules = roomSchedules.Where(s => s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")).Select(s => ExtractTimeSpan(s.TimeSlot)).Where(t => t.HasValue).Select(t => t.Value).OrderBy(t => t.Start).ToList();

                // 2. Calculate gaps for each group
                string mwfAvailability = CalculateAvailability(mwfSchedules);
                string tthAvailability = CalculateAvailability(tthSchedules);

                // 3. Format the final string for the table
                Rooms.Add(new RoomDisplay
                {
                    RoomId = room.RoomId,
                    Name = room.Name,
                    Type = room.Type,
                    Capacity = room.Capacity,
                    Availability = $"MWF: {mwfAvailability} | TTh: {tthAvailability}"
                });
            }
        }

        // Keep your existing ExtractTimeSpan, CalculateAvailability, and FormatTime methods!

        // --- 1. Checks if the class happens on the current real-world day ---
        private bool IsScheduledToday(string timeSlot, DayOfWeek today)
        {
            string daysPart = timeSlot.Split(' ')[0]; // Gets "MWF" or "TTh"

            return today switch
            {
                DayOfWeek.Monday => daysPart.Contains("M"),
                DayOfWeek.Tuesday => daysPart.Replace("Th", "").Contains("T"), // Prevents 'Th' from triggering 'T'
                DayOfWeek.Wednesday => daysPart.Contains("W"),
                DayOfWeek.Thursday => daysPart.Contains("Th"),
                DayOfWeek.Friday => daysPart.Contains("F"),
                DayOfWeek.Saturday => daysPart.Contains("S"),
                _ => false
            };
        }

        // --- 2. Extracts actual Time math from text like "1:00 PM - 2:00 PM" ---
        private (TimeSpan Start, TimeSpan End)? ExtractTimeSpan(string timeSlot)
        {
            var match = Regex.Match(timeSlot, @"(\d{1,2}:\d{2}\s*[aA][mMpP][mM]?)\s*-\s*(\d{1,2}:\d{2}\s*[aA][mMpP][mM]?)");
            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out DateTime startTime) &&
                    DateTime.TryParse(match.Groups[2].Value, out DateTime endTime))
                {
                    return (startTime.TimeOfDay, endTime.TimeOfDay);
                }
            }
            return null; // Return null if it's malformed like "TBA"
        }

        // --- 3. The Algorithm: Gap finding between 7 AM and 9 PM ---
        private string CalculateAvailability(List<(TimeSpan Start, TimeSpan End)> schedules)
        {
            TimeSpan dayStart = new TimeSpan(7, 0, 0);  // 7:00 AM
            TimeSpan dayEnd = new TimeSpan(21, 0, 0);   // 9:00 PM
            TimeSpan currentTime = dayStart;

            List<string> availableSlots = new List<string>();

            foreach (var s in schedules)
            {
                if (currentTime < s.Start)
                {
                    availableSlots.Add($"{FormatTime(currentTime)} - {FormatTime(s.Start)}");
                }

                if (currentTime < s.End)
                {
                    currentTime = s.End;
                }
            }

            if (currentTime < dayEnd)
            {
                availableSlots.Add($"{FormatTime(currentTime)} - {FormatTime(dayEnd)}");
            }

            if (availableSlots.Count == 0) return "Fully Booked Today";

            return string.Join(", ", availableSlots);
        }

        private string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("h:mm tt");
        }

        // ==========================================
        // ADD, EDIT, DELETE METHODS
        // ==========================================

        public IActionResult OnPostAddRoom()
        {
            if (!string.IsNullOrEmpty(NewRoomName) && !string.IsNullOrEmpty(NewRoomType) && NewRoomCapacity > 0)
            {
                var room = new Models.Room
                {
                    Name = NewRoomName,
                    Type = NewRoomType,
                    Capacity = NewRoomCapacity
                };

                try
                {
                    _db.Rooms.Add(room);
                    AddAuditEntryToContext($"Created room: {NewRoomName}");
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully added!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred. {ex.Message}";
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteRoom(int roomId)
        {
            var room = _db.Rooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
            {
                try
                {
                    _db.Rooms.Remove(room);
                    AddAuditEntryToContext($"Deleted room: {room.Name}");
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully deleted!";
                }
                catch (Exception ex) { TempData["ErrorMessage"] = $"An error occurred. {ex.Message}"; }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostEditRoom()
        {
            var room = _db.Rooms.FirstOrDefault(r => r.RoomId == EditRoomId);
            if (room != null)
            {
                room.Name = EditRoomName;
                room.Type = EditRoomType;
                room.Capacity = EditRoomCapacity;

                try
                {
                    AddAuditEntryToContext($"Updated room: {room.Name}");
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully updated!";
                }
                catch (Exception ex) { TempData["ErrorMessage"] = $"An error occurred. {ex.Message}"; }
            }
            return RedirectToPage();
        }

        private void AddAuditEntryToContext(string action)
        {
            var facultyId = HttpContext.Session.GetString("ActiveUser");
            int? userId = null;

            if (!string.IsNullOrEmpty(facultyId))
            {
                var user = _db.Users.FirstOrDefault(u => u.FacultyId == facultyId);
                if (user != null) userId = user.Id;
            }

            _db.AuditLogs.Add(new AuditLog { Timestamp = DateTime.UtcNow, UserId = userId, Action = action });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        // Rooms pulled from the database
        public List<Models.Room> Rooms { get; set; } = new List<Models.Room>();

        // --- PROPERTIES FOR ADDING A ROOM ---
        [BindProperty] public string NewRoomName { get; set; }
        [BindProperty] public string NewRoomType { get; set; }
        [BindProperty] public int NewRoomCapacity { get; set; }

        // --- PROPERTIES FOR EDITING A ROOM ---
        [BindProperty] public int EditRoomId { get; set; }
        [BindProperty] public string EditRoomName { get; set; }
        [BindProperty] public string EditRoomType { get; set; }
        [BindProperty] public int EditRoomCapacity { get; set; }
        [BindProperty] public string EditRoomAvailability { get; set; }

        public void OnGet()
        {
            // Load rooms from the database (order by room name)
            Rooms = _db.Rooms.OrderBy(r => r.Name).ToList();
        }

        // 2. Add Room Method
        public IActionResult OnPostAddRoom()
        {
            if (!string.IsNullOrEmpty(NewRoomName) && !string.IsNullOrEmpty(NewRoomType) && NewRoomCapacity > 0)
            {
                var room = new Models.Room
                {
                    // store the provided room name in the `room_name` column
                    Name = NewRoomName,
                    Type = NewRoomType,
                    Capacity = NewRoomCapacity,
                    Availability = "Available"
                };

                // Add both room and audit entry then save once to ensure atomicity
                try
                {
                    _db.Rooms.Add(room);
                    AddAuditEntryToContext($"Created room: {NewRoomName}");
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully added!";
                }
                catch (Exception ex)
                {
                    // Log exception for troubleshooting and show detailed message in TempData for debugging
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error adding room: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while saving the room. {baseMsg}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all fields correctly.";
            }
            return RedirectToPage();
        }

        // 3. Delete Room Method (by room name)
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
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error deleting room: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while deleting the room. {baseMsg}";
                }
            }
            return RedirectToPage();
        }

        // 4. Edit Room Method (find by original name and update)
        public IActionResult OnPostEditRoom()
        {
            var room = _db.Rooms.FirstOrDefault(r => r.RoomId == EditRoomId);
            if (room != null)
            {
                room.Name = EditRoomName;
                room.Type = EditRoomType;
                room.Capacity = EditRoomCapacity;
                room.Availability = EditRoomAvailability;

                try
                {
                    AddAuditEntryToContext($"Updated room: {room.Name}");
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully updated!";
                }
                catch (Exception ex)
                {
                    var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error updating room: {baseMsg}");
                    TempData["ErrorMessage"] = $"An error occurred while updating the room. {baseMsg}";
                }
            }

            return RedirectToPage();
        }

        private void AddAuditEntryToContext(string action)
        {
            // Get the currently logged-in user's FacultyId from session
            var facultyId = HttpContext.Session.GetString("ActiveUser");
            int? userId = null;

            if (!string.IsNullOrEmpty(facultyId))
            {
                // Look up the user ID from the database using FacultyId
                var user = _db.Users.FirstOrDefault(u => u.FacultyId == facultyId);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var auditLog = new AuditLog
            {
                // Use UTC to match PostgreSQL 'timestamp with time zone' requirements
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Action = action
            };

            // Add to context but do NOT call SaveChanges here. Caller will persist.
            _db.AuditLogs.Add(auditLog);
        }
    }

}

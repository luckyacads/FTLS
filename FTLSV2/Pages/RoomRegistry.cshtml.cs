using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages
{
    public class RoomRegistryModel : PageModel
    {
        // 1. Temporary list to store our rooms (Simulated Database)
        public static List<RoomModel> Rooms { get; set; } = new List<RoomModel>
        {
            // We use Guid.NewGuid() to give every room a unique invisible ID for editing and deleting
            new RoomModel { Id = Guid.NewGuid().ToString(), Name = "Room A101", Type = "Lecture", Capacity = 40, Availability = "Available" },
            new RoomModel { Id = Guid.NewGuid().ToString(), Name = "Lab B202", Type = "Lab", Capacity = 24, Availability = "Booked" }
        };

        // --- PROPERTIES FOR ADDING A ROOM ---
        [BindProperty] public string NewRoomName { get; set; }
        [BindProperty] public string NewRoomType { get; set; }
        [BindProperty] public int NewRoomCapacity { get; set; }

        // --- PROPERTIES FOR EDITING A ROOM ---
        [BindProperty] public string EditRoomId { get; set; }
        [BindProperty] public string EditRoomName { get; set; }
        [BindProperty] public string EditRoomType { get; set; }
        [BindProperty] public int EditRoomCapacity { get; set; }
        [BindProperty] public string EditRoomAvailability { get; set; }

        public void OnGet()
        {
        }

        // 2. Add Room Method
        public IActionResult OnPostAddRoom()
        {
            if (!string.IsNullOrEmpty(NewRoomName))
            {
                // 1. Add the room to the Room list
                Rooms.Add(new RoomModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = NewRoomName,
                    Type = NewRoomType,
                    Capacity = NewRoomCapacity,
                    Availability = "Available"
                });

                // 2. NEW: Send a record to the Audit Logs!
                AuditLogsModel.Logs.Insert(0, new LogEntry
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), // Gets exact current time
                    Username = "Admin", // Hardcoded until you build a real login system
                    Action = $"Created new room: {NewRoomName}",
                    IPAddress = "127.0.0.1" // Localhost IP
                });

                TempData["SuccessMessage"] = "Room successfully added!";
            }
            return RedirectToPage();
        }

        // 3. Delete Room Method
        public IActionResult OnPostDeleteRoom(string roomId)
        {
            var roomToRemove = Rooms.FirstOrDefault(r => r.Id == roomId);
            if (roomToRemove != null)
            {
                Rooms.Remove(roomToRemove);
                TempData["SuccessMessage"] = "Room successfully deleted!";
            }
            return RedirectToPage();
        }

        // 4. Edit Room Method
        public IActionResult OnPostEditRoom()
        {
            var roomToEdit = Rooms.FirstOrDefault(r => r.Id == EditRoomId);
            if (roomToEdit != null)
            {
                roomToEdit.Name = EditRoomName;
                roomToEdit.Type = EditRoomType;
                roomToEdit.Capacity = EditRoomCapacity;
                roomToEdit.Availability = EditRoomAvailability;

                TempData["SuccessMessage"] = "Room successfully updated!";
            }
            return RedirectToPage();
        }
    }

    // Class defining what a Room looks like
    public class RoomModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Availability { get; set; }
    }
}
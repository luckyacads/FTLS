using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FTLSV2.Pages
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
            if (!string.IsNullOrEmpty(NewRoomName))
            {
                var room = new Models.Room
                {
                    // store the provided room name in the `room_name` column
                    Name = NewRoomName,
                    Type = NewRoomType,
                    Capacity = NewRoomCapacity,
                    Availability = "Available"
                };
                // If you need to set a specific RoomId, ensure it does not conflict with the DB sequence
                _db.Rooms.Add(room);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Room successfully added!";
            }
            return RedirectToPage();
        }

        // 3. Delete Room Method (by room name)
        public IActionResult OnPostDeleteRoom(int roomId)
        {
            var room = _db.Rooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
                {
                    _db.Rooms.Remove(room);
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully deleted!";
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
                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Room successfully updated!";
                }

            return RedirectToPage();
        }
    }

}
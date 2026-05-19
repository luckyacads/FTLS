using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Rooms = _db.Rooms
                .OrderBy(r => r.Name)
                .Select(r => new RoomDisplay
                {
                    RoomId = r.RoomId,
                    Name = r.Name,
                    Type = r.Type,
                    Capacity = r.Capacity
                })
                .ToList();
        }

        public IActionResult OnPostAddRoom()
        {
            if (!string.IsNullOrWhiteSpace(NewRoomName) &&
                !string.IsNullOrWhiteSpace(NewRoomType) &&
                NewRoomCapacity > 0)
            {
                var room = new Models.Room
                {
                    Name = NewRoomName.Trim(),
                    Type = NewRoomType.Trim(),
                    Capacity = NewRoomCapacity
                };

                try
                {
                    _db.Rooms.Add(room);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Room successfully added!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please provide a valid room name, type, and capacity.";
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
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Room successfully deleted!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Room not found.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostEditRoom()
        {
            var room = _db.Rooms.FirstOrDefault(r => r.RoomId == EditRoomId);

            if (room != null)
            {
                if (string.IsNullOrWhiteSpace(EditRoomName) ||
                    string.IsNullOrWhiteSpace(EditRoomType) ||
                    EditRoomCapacity <= 0)
                {
                    TempData["ErrorMessage"] = "Please provide a valid room name, type, and capacity.";
                    return RedirectToPage();
                }

                room.Name = EditRoomName.Trim();
                room.Type = EditRoomType.Trim();
                room.Capacity = EditRoomCapacity;

                try
                {
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Room successfully updated!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Room not found.";
            }

            return RedirectToPage();
        }
    }
}
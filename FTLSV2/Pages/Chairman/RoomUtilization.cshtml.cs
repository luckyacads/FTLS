using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;

namespace FTLSV2.Pages.Chairman
{
    public class RoomUtilizationModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public RoomUtilizationModel(FtlsDbContext db)
        {
            _db = db;
        }

        // View model for rooms shown in the UI
        public record RoomView(string Name, string Status, int Capacity);

        public List<RoomView> Rooms { get; set; } = new();

        public void OnGet()
        {
            // Load rooms from the room_registry table
            Rooms = _db.Rooms
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoomView(r.Name, r.Availability, r.Capacity))
                .ToList();
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class RoomUtilizationModel : PageModel
    {
        public record Room(string Name, string Status, int Capacity);

        public List<Room> Rooms { get; set; } = new();

        public void OnGet()
        {
            Rooms = new List<Room>
            {
                new Room("Lab A","Available",40),
                new Room("Room 101","Booked",30),
                new Room("Audio Visual Room","In Use",25),
            };
        }
    }
}

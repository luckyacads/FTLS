using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public record RoomView(string Name, string Availability, int Capacity);

        public List<RoomView> Rooms { get; set; } = new();

        public void OnGet()
        {
            var allRooms = _db.Rooms.OrderBy(r => r.Name).ToList();
            var allSchedules = _db.Schedules.ToList();

            foreach (var room in allRooms)
            {
                var roomSchedules = allSchedules.Where(s => s.RoomId == room.RoomId && !string.IsNullOrEmpty(s.TimeSlot)).ToList();

                // Group schedules by MWF and TTh
                var mwfSchedules = roomSchedules.Where(s => s.TimeSlot.StartsWith("MWF")).Select(s => ExtractTimeSpan(s.TimeSlot)).Where(t => t.HasValue).Select(t => t.Value).OrderBy(t => t.Start).ToList();
                var tthSchedules = roomSchedules.Where(s => s.TimeSlot.StartsWith("TTh") || s.TimeSlot.StartsWith("TTH")).Select(s => ExtractTimeSpan(s.TimeSlot)).Where(t => t.HasValue).Select(t => t.Value).OrderBy(t => t.Start).ToList();

                // Calculate gaps for each group
                string mwfAvailability = CalculateAvailability(mwfSchedules);
                string tthAvailability = CalculateAvailability(tthSchedules);

                // Format the final string for the table
                string availability = $"MWF: {mwfAvailability} | TTh: {tthAvailability}";

                Rooms.Add(new RoomView(room.Name, availability, room.Capacity));
            }
        }

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
            return null;
        }

        private string CalculateAvailability(List<(TimeSpan Start, TimeSpan End)> schedules)
        {
            if (schedules.Count == 0)
                return "7:00 AM - 9:00 PM";

            var gaps = new List<(TimeSpan Start, TimeSpan End)>();
            var dayStart = TimeSpan.FromHours(7); // 7:00 AM
            var dayEnd = TimeSpan.FromHours(21); // 9:00 PM

            if (schedules[0].Start > dayStart)
                gaps.Add((dayStart, schedules[0].Start));

            for (int i = 0; i < schedules.Count - 1; i++)
            {
                if (schedules[i].End < schedules[i + 1].Start)
                    gaps.Add((schedules[i].End, schedules[i + 1].Start));
            }

            if (schedules.Last().End < dayEnd)
                gaps.Add((schedules.Last().End, dayEnd));

            if (gaps.Count == 0)
                return "Fully booked";

            return string.Join(", ", gaps.Select(g => $"{FormatTime(g.Start)} - {FormatTime(g.End)}"));
        }

        private string FormatTime(TimeSpan time)
        {
            return time.Hours switch
            {
                < 12 => $"{time.Hours}:{time.Minutes:D2} AM",
                12 => $"12:{time.Minutes:D2} PM",
                _ => $"{time.Hours - 12}:{time.Minutes:D2} PM"
            };
        }
    }
}

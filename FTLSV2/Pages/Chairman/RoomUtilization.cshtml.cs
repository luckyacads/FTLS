using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        [BindProperty(SupportsGet = true)]
        public string SelectedDay { get; set; } = "Monday";

        [BindProperty(SupportsGet = true)]
        public string SelectedYear { get; set; } = "2024-2025";

        public class TimeBlock
        {
            public string Label { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public bool IsOccupied { get; set; }
            public double DurationMinutes { get; set; }
        }

        public class RoomDisplay
        {
            public string RoomName { get; set; }
            public int Capacity { get; set; }
            public List<TimeBlock> Timeline { get; set; } = new List<TimeBlock>();
        }

        public List<RoomDisplay> Rooms { get; set; } = new List<RoomDisplay>();

        public void OnGet()
        {
            var allRooms = _db.Rooms.OrderBy(r => r.Name).ToList();
            var allSchedules = _db.Schedules.ToList();

            // ---> NEW: We load the subjects so we can cross-reference the SubjectId <---
            var allSubjects = _db.Subjects.ToList();

            foreach (var room in allRooms)
            {
                var roomSchedulesForDay = allSchedules
                    .Where(s => s.RoomId == room.RoomId && IsScheduledToday(s.TimeSlot, SelectedDay))
                    .Select(s => new
                    {
                        // ---> NEW: Find the matching subject title using the SubjectId <---
                        Title = allSubjects.FirstOrDefault(sub => sub.SubjectId == s.SubjectId)?.Title ?? "Occupied",
                        Time = ExtractTimeSpan(s.TimeSlot)
                    })
                    .Where(s => s.Time.HasValue)
                    .Select(s => new { s.Title, Start = s.Time.Value.Start, End = s.Time.Value.End })
                    .OrderBy(s => s.Start)
                    .ToList();

                Rooms.Add(new RoomDisplay
                {
                    RoomName = room.Name,
                    Capacity = room.Capacity,
                    Timeline = GenerateTimelineBlocks(roomSchedulesForDay)
                });
            }
        }

        private bool IsScheduledToday(string timeSlot, string selectedDay)
        {
            if (string.IsNullOrEmpty(timeSlot)) return false;

            string daysPart = timeSlot.Split(' ')[0].ToUpper();
            return selectedDay switch
            {
                "Monday" => daysPart.Contains("M"),
                "Tuesday" => daysPart.Replace("TH", "").Contains("T"),
                "Wednesday" => daysPart.Contains("W"),
                "Thursday" => daysPart.Contains("TH"),
                "Friday" => daysPart.Contains("F"),
                "Saturday" => daysPart.Contains("S"),
                _ => false
            };
        }

        private List<TimeBlock> GenerateTimelineBlocks(dynamic schedules)
        {
            var timeline = new List<TimeBlock>();
            var dayStart = TimeSpan.FromHours(7); // 7:00 AM
            var dayEnd = TimeSpan.FromHours(21); // 9:00 PM

            TimeSpan currentTime = dayStart;

            foreach (var schedule in schedules)
            {
                if (currentTime < schedule.Start)
                {
                    timeline.Add(CreateBlock("Vacant", currentTime, schedule.Start, false));
                }

                timeline.Add(CreateBlock(schedule.Title, schedule.Start, schedule.End, true));
                currentTime = schedule.End;
            }

            if (currentTime < dayEnd)
            {
                timeline.Add(CreateBlock("Vacant", currentTime, dayEnd, false));
            }

            return timeline;
        }

        private TimeBlock CreateBlock(string label, TimeSpan start, TimeSpan end, bool isOccupied)
        {
            return new TimeBlock
            {
                Label = label,
                StartTime = FormatTime(start),
                EndTime = FormatTime(end),
                IsOccupied = isOccupied,
                DurationMinutes = (end - start).TotalMinutes
            };
        }

        private (TimeSpan Start, TimeSpan End)? ExtractTimeSpan(string timeSlot)
        {
            var match = Regex.Match(timeSlot, @"(\d{1,2}:\d{2}\s*[aApP][mM])\s*-\s*(\d{1,2}:\d{2}\s*[aApP][mM])", RegexOptions.IgnoreCase);
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

        private string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("h:mm tt").ToLower();
        }
    }
}
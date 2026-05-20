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

        private readonly TimeSpan CalendarStart = TimeSpan.FromHours(7);   // 7:00 AM
        private readonly TimeSpan CalendarEnd = TimeSpan.FromHours(21);    // 9:00 PM

        private const double PixelsPerMinute = 1.8;
        private const double MinimumEventHeightPixels = 90;

        public RoomUtilizationModel(FtlsDbContext db)
        {
            _db = db;
        }

        public double CalendarHeightPixels { get; set; }

        public double HalfHourHeightPixels { get; set; }

        public List<TimeMarker> TimeMarkers { get; set; } = new List<TimeMarker>();

        public List<CalendarDayHeader> CalendarDays { get; set; } = new List<CalendarDayHeader>();

        public List<RoomDisplay> Rooms { get; set; } = new List<RoomDisplay>();

        public class CalendarDayHeader
        {
            public string Key { get; set; }
            public string ShortLabel { get; set; }
            public string Label { get; set; }
        }

        public class TimeMarker
        {
            public string Label { get; set; }
            public double TopPixels { get; set; }
        }

        public class ScheduleEvent
        {
            public string DayKey { get; set; }
            public string Title { get; set; }
            public string StartTimeText { get; set; }
            public string EndTimeText { get; set; }
            public double TopPixels { get; set; }
            public double HeightPixels { get; set; }
        }

        public class CalendarDayColumn
        {
            public string Key { get; set; }
            public string ShortLabel { get; set; }
            public string Label { get; set; }
            public List<ScheduleEvent> Events { get; set; } = new List<ScheduleEvent>();
        }

        public class RoomDisplay
        {
            public string RoomName { get; set; }
            public int Capacity { get; set; }
            public int TotalEvents { get; set; }
            public List<CalendarDayColumn> WeekDays { get; set; } = new List<CalendarDayColumn>();
        }

        public void OnGet()
        {
            CalendarDays = GetCalendarDays();
            TimeMarkers = GenerateTimeMarkers();

            CalendarHeightPixels = (CalendarEnd - CalendarStart).TotalMinutes * PixelsPerMinute;
            HalfHourHeightPixels = 30 * PixelsPerMinute;

            var allRooms = _db.Rooms
                .OrderBy(r => r.Name)
                .ToList();

            var allSchedules = _db.Schedules
                .ToList();

            var allSubjects = _db.Subjects
                .ToList();

            foreach (var room in allRooms)
            {
                var weekDays = CalendarDays
                    .Select(day => new CalendarDayColumn
                    {
                        Key = day.Key,
                        ShortLabel = day.ShortLabel,
                        Label = day.Label,
                        Events = new List<ScheduleEvent>()
                    })
                    .ToList();

                var roomSchedules = allSchedules
                    .Where(schedule => schedule.RoomId == room.RoomId)
                    .ToList();

                foreach (var schedule in roomSchedules)
                {
                    var timeRange = ExtractTimeSpan(schedule.TimeSlot);

                    if (!timeRange.HasValue)
                    {
                        continue;
                    }

                    var scheduledDays = ExtractScheduledDays(schedule.TimeSlot);

                    if (!scheduledDays.Any())
                    {
                        continue;
                    }

                    var subjectTitle = allSubjects
                        .FirstOrDefault(subject => subject.SubjectId == schedule.SubjectId)
                        ?.Title ?? "Occupied";

                    foreach (var dayKey in scheduledDays)
                    {
                        var dayColumn = weekDays.FirstOrDefault(day => day.Key == dayKey);

                        if (dayColumn == null)
                        {
                            continue;
                        }

                        var scheduleEvent = CreateScheduleEvent(
                            dayKey,
                            subjectTitle,
                            timeRange.Value.Start,
                            timeRange.Value.End
                        );

                        dayColumn.Events.Add(scheduleEvent);
                    }
                }

                foreach (var day in weekDays)
                {
                    day.Events = day.Events
                        .OrderBy(e => e.TopPixels)
                        .ToList();
                }

                Rooms.Add(new RoomDisplay
                {
                    RoomName = room.Name,
                    Capacity = room.Capacity,
                    TotalEvents = weekDays.Sum(day => day.Events.Count),
                    WeekDays = weekDays
                });
            }
        }

        private List<CalendarDayHeader> GetCalendarDays()
        {
            return new List<CalendarDayHeader>
            {
                new CalendarDayHeader { Key = "Monday", ShortLabel = "MON", Label = "Monday" },
                new CalendarDayHeader { Key = "Tuesday", ShortLabel = "TUE", Label = "Tuesday" },
                new CalendarDayHeader { Key = "Wednesday", ShortLabel = "WED", Label = "Wednesday" },
                new CalendarDayHeader { Key = "Thursday", ShortLabel = "THU", Label = "Thursday" },
                new CalendarDayHeader { Key = "Friday", ShortLabel = "FRI", Label = "Friday" },
                new CalendarDayHeader { Key = "Saturday", ShortLabel = "SAT", Label = "Saturday" }
            };
        }

        private List<TimeMarker> GenerateTimeMarkers()
        {
            var markers = new List<TimeMarker>();
            var currentTime = CalendarStart;

            while (currentTime <= CalendarEnd)
            {
                markers.Add(new TimeMarker
                {
                    Label = FormatTime(currentTime),
                    TopPixels = (currentTime - CalendarStart).TotalMinutes * PixelsPerMinute
                });

                currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
            }

            return markers;
        }

        private ScheduleEvent CreateScheduleEvent(string dayKey, string title, TimeSpan start, TimeSpan end)
        {
            var clampedStart = start < CalendarStart ? CalendarStart : start;
            var clampedEnd = end > CalendarEnd ? CalendarEnd : end;

            if (clampedEnd <= clampedStart)
            {
                clampedEnd = clampedStart.Add(TimeSpan.FromMinutes(30));
            }

            var topPixels = (clampedStart - CalendarStart).TotalMinutes * PixelsPerMinute;
            var heightPixels = Math.Max(
                MinimumEventHeightPixels,
                (clampedEnd - clampedStart).TotalMinutes * PixelsPerMinute
            );

            return new ScheduleEvent
            {
                DayKey = dayKey,
                Title = title,
                StartTimeText = FormatTime(start),
                EndTimeText = FormatTime(end),
                TopPixels = topPixels,
                HeightPixels = heightPixels
            };
        }

        private List<string> ExtractScheduledDays(string timeSlot)
        {
            var days = new List<string>();

            if (string.IsNullOrWhiteSpace(timeSlot))
            {
                return days;
            }

            var firstPart = timeSlot
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstPart))
            {
                return days;
            }

            var dayCode = Regex.Replace(firstPart.ToUpper(), @"[^A-Z]", "");

            if (dayCode.Contains("MONDAY"))
            {
                days.Add("Monday");
            }

            if (dayCode.Contains("TUESDAY"))
            {
                days.Add("Tuesday");
            }

            if (dayCode.Contains("WEDNESDAY"))
            {
                days.Add("Wednesday");
            }

            if (dayCode.Contains("THURSDAY"))
            {
                days.Add("Thursday");
            }

            if (dayCode.Contains("FRIDAY"))
            {
                days.Add("Friday");
            }

            if (dayCode.Contains("SATURDAY"))
            {
                days.Add("Saturday");
            }

            if (days.Any())
            {
                return OrderDays(days);
            }

            if (dayCode.Contains("M"))
            {
                days.Add("Monday");
            }

            if (dayCode.Contains("TH"))
            {
                days.Add("Thursday");
            }

            var codeWithoutThursday = dayCode.Replace("TH", "");

            if (codeWithoutThursday.Contains("T"))
            {
                days.Add("Tuesday");
            }

            if (codeWithoutThursday.Contains("W"))
            {
                days.Add("Wednesday");
            }

            if (codeWithoutThursday.Contains("F"))
            {
                days.Add("Friday");
            }

            if (codeWithoutThursday.Contains("S"))
            {
                days.Add("Saturday");
            }

            return OrderDays(days);
        }

        private List<string> OrderDays(List<string> days)
        {
            var order = new List<string>
            {
                "Monday",
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
            };

            return days
                .Distinct()
                .OrderBy(day => order.IndexOf(day))
                .ToList();
        }

        private (TimeSpan Start, TimeSpan End)? ExtractTimeSpan(string timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot))
            {
                return null;
            }

            var match = Regex.Match(
                timeSlot,
                @"(\d{1,2}:\d{2}\s*[aApP][mM])\s*-\s*(\d{1,2}:\d{2}\s*[aApP][mM])",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
            {
                return null;
            }

            if (DateTime.TryParse(match.Groups[1].Value, out DateTime startTime) &&
                DateTime.TryParse(match.Groups[2].Value, out DateTime endTime))
            {
                return (startTime.TimeOfDay, endTime.TimeOfDay);
            }

            return null;
        }

        private string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("h:mm tt");
        }
    }
}
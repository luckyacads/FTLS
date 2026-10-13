using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.RegularExpressions;

namespace FTLSV2.Pages.Chairman
{
    public class MyScheduleModel : PageModel
    {
        private readonly FtlsDbContext _db;

        private readonly TimeSpan CalendarStart = TimeSpan.FromHours(7);   // 7:00 AM
        private readonly TimeSpan CalendarEnd = TimeSpan.FromHours(21);    // 9:00 PM

        private const double PixelsPerMinute = 1.8;
        private const double MinimumEventHeightPixels = 54;

        public MyScheduleModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record ScheduleView(string TimeSlot, string SubjectCode, string SubjectTitle, string RoomName, string Units, int? OfferCode);

        // MATCHING TEACHER PORTAL
        public record SubjectGroup(string HeaderName, List<SubjectDetail> Subjects);
        public record SubjectDetail(string SubjectCode, string SubjectTitle, string Units, int? OfferCode);

        public List<ScheduleView> MWFSchedules { get; set; } = new();
        public List<ScheduleView> TTHSchedules { get; set; } = new();

        public List<SubjectGroup> SubjectHistory { get; set; } = new();
        public List<string> AvailableYears { get; set; } = new();

        public int TotalUnits { get; set; }
        public int TotalClasses { get; set; }
        public string CurrentAcademicYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedYear { get; set; }

        // --- ROOM UTILIZATION STYLE TIMETABLE GRID PROPERTIES ---
        public double CalendarHeightPixels { get; set; }
        public double HalfHourHeightPixels { get; set; }
        public List<TimeMarker> TimeMarkers { get; set; } = new();
        public List<CalendarDayColumn> WeekDays { get; set; } = new();

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
            public string SubjectCode { get; set; }
            public string Title { get; set; }
            public string StartTimeText { get; set; }
            public string EndTimeText { get; set; }
            public string RoomName { get; set; }
            public string Units { get; set; }
            public int? OfferCode { get; set; }
            public double TopPixels { get; set; }
            public double HeightPixels { get; set; }
        }

        public class CalendarDayColumn
        {
            public string Key { get; set; }
            public string ShortLabel { get; set; }
            public string Label { get; set; }
            public List<ScheduleEvent> Events { get; set; } = new();
        }

        public void OnGet()
        {
            // Initialize Calendar geometry and columns
            CalendarHeightPixels = (CalendarEnd - CalendarStart).TotalMinutes * PixelsPerMinute;
            HalfHourHeightPixels = 30 * PixelsPerMinute;
            TimeMarkers = GenerateTimeMarkers();

            var calendarDays = GetCalendarDays();
            WeekDays = calendarDays
                .Select(day => new CalendarDayColumn
                {
                    Key = day.Key,
                    ShortLabel = day.ShortLabel,
                    Label = day.Label,
                    Events = new List<ScheduleEvent>()
                })
                .ToList();

            var chairmanFacultyIdString = HttpContext.Session.GetString("ActiveUser");
            if (string.IsNullOrEmpty(chairmanFacultyIdString))
            {
                return;
            }

            int chairmanFacultyId = int.Parse(chairmanFacultyIdString);

            var user = _db.Users.FirstOrDefault(u => u.FacultyId == chairmanFacultyId);
            if (user == null)
            {
                return;
            }

            // DYNAMIC CHOICE GENERATION: Automatically build years based on calendar clocks
            int currentYear = DateTime.Today.Year;
            AvailableYears = new List<string>();
            for (int year = 2023; year <= currentYear + 1; year++)
            {
                AvailableYears.Add($"{year}-{year + 1}");
            }
            AvailableYears = AvailableYears.OrderByDescending(y => y).ToList();

            if (string.IsNullOrEmpty(SelectedYear))
            {
                SelectedYear = $"{currentYear}-{currentYear + 1}";
            }

            CurrentAcademicYear = (SelectedYear == "All") ? $"{currentYear}-{currentYear + 1}" : SelectedYear;

            // Filter schedules for the Weekly Schedule view and totals based on the SelectedYear
            var schedulesQuery = _db.Schedules
                .Where(s => s.FacultyId == user.FacultyId);

            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                schedulesQuery = schedulesQuery.Where(s => s.AcademicYear == SelectedYear);
            }
            else if (SelectedYear == "All")
            {
                string defaultCurrentYear = $"{currentYear}-{currentYear + 1}";
                schedulesQuery = schedulesQuery.Where(s => s.AcademicYear == defaultCurrentYear);
            }

            var schedules = schedulesQuery.AsNoTracking().ToList();

            // Total stats
            TotalClasses = schedules.Count;
            TotalUnits = schedules.Sum(s => s.AssignedUnits);

            if (schedules.Any())
            {
                var roomIds = schedules
                    .Where(s => s.RoomId.HasValue)
                    .Select(s => s.RoomId!.Value)
                    .Distinct()
                    .ToList();

                var subjectIds = schedules.Select(s => s.SubjectId).Distinct().ToList();

                var rooms = _db.Rooms
                    .Where(r => roomIds.Contains(r.RoomId))
                    .AsNoTracking()
                    .ToDictionary(r => r.RoomId);

                var subjects = _db.Subjects
                    .Where(s => subjectIds.Contains(s.SubjectId))
                    .AsNoTracking()
                    .ToDictionary(s => s.SubjectId);

                // Populate timetable events into week days
                foreach (var schedule in schedules)
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

                    var subject = subjects.ContainsKey(schedule.SubjectId) ? subjects[schedule.SubjectId] : null;
                    var subjectTitle = subject?.Title ?? "Occupied";
                    var subjectCode = subject?.Code ?? "N/A";
                    var roomName = (schedule.RoomId.HasValue && rooms.ContainsKey(schedule.RoomId.Value)) ? rooms[schedule.RoomId.Value].Name : "TBA";

                    foreach (var dayKey in scheduledDays)
                    {
                        var dayColumn = WeekDays.FirstOrDefault(d => d.Key.Equals(dayKey, StringComparison.OrdinalIgnoreCase));
                        if (dayColumn == null)
                        {
                            continue;
                        }

                        var evt = CreateScheduleEvent(
                            dayKey,
                            subjectCode,
                            subjectTitle,
                            roomName,
                            schedule.AssignedUnits.ToString(),
                            schedule.OfferCode,
                            timeRange.Value.Start,
                            timeRange.Value.End
                        );

                        dayColumn.Events.Add(evt);
                    }
                }

                foreach (var day in WeekDays)
                {
                    day.Events = day.Events
                        .OrderBy(e => e.TopPixels)
                        .ToList();
                }

                // Populate MWFSchedules and TTHSchedules for compatibility
                Func<string, bool> isMWF = timeSlot =>
                {
                    if (string.IsNullOrWhiteSpace(timeSlot)) return false;
                    int firstDigit = timeSlot.IndexOfAny("0123456789".ToCharArray());
                    string daysPart = firstDigit > 0 ? timeSlot.Substring(0, firstDigit) : timeSlot;
                    var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                    return tokens.Any(t => t == "M" || t == "W" || t == "F" || t == "S" ||
                                           t.Equals("MWF", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Mon", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Wed", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Fri", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Sat", StringComparison.OrdinalIgnoreCase));
                };

                Func<string, bool> isTTH = timeSlot =>
                {
                    if (string.IsNullOrWhiteSpace(timeSlot)) return false;
                    int firstDigit = timeSlot.IndexOfAny("0123456789".ToCharArray());
                    string daysPart = firstDigit > 0 ? timeSlot.Substring(0, firstDigit) : timeSlot;
                    var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                    return tokens.Any(t => t == "T" || t == "Th" ||
                                           t.Equals("TTH", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("TTh", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Tue", StringComparison.OrdinalIgnoreCase) ||
                                           t.Equals("Thu", StringComparison.OrdinalIgnoreCase));
                };

                MWFSchedules = schedules
                    .Where(s => isMWF(s.TimeSlot))
                    .Select(s => new ScheduleView(
                        s.TimeSlot,
                        subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                        subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                        (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                        s.AssignedUnits.ToString(),
                        s.OfferCode
                    ))
                    .OrderBy(s => ParseStartTime(s.TimeSlot))
                    .ToList();

                TTHSchedules = schedules
                    .Where(s => isTTH(s.TimeSlot))
                    .Select(s => new ScheduleView(
                        s.TimeSlot,
                        subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Code : "N/A",
                        subjects.ContainsKey(s.SubjectId) ? subjects[s.SubjectId].Title : "N/A",
                        (s.RoomId.HasValue && rooms.ContainsKey(s.RoomId.Value)) ? rooms[s.RoomId.Value].Name : "TBA",
                        s.AssignedUnits.ToString(),
                        s.OfferCode
                    ))
                    .OrderBy(s => ParseStartTime(s.TimeSlot))
                    .ToList();
            }

            // Subject History
            var allSchedulesWithSubjects = _db.Schedules
                .Where(s => s.FacultyId == user.FacultyId)
                .Join(_db.Subjects, s => s.SubjectId, sub => sub.SubjectId, (s, sub) => new { Schedule = s, Subject = sub })
                .AsNoTracking()
                .ToList();

            var filteredSubjects = allSchedulesWithSubjects;
            if (!string.IsNullOrEmpty(SelectedYear) && SelectedYear != "All")
            {
                filteredSubjects = filteredSubjects
                    .Where(ps => (ps.Schedule.AcademicYear ?? "N/A") == SelectedYear)
                    .ToList();
            }

            SubjectHistory = filteredSubjects
                .GroupBy(ps => $"Academic Year {ps.Schedule.AcademicYear ?? "TBA"}")
                .Select(g => new SubjectGroup(
                    g.Key,
                    g.Select(ps => new SubjectDetail(
                        ps.Subject.Code,
                        ps.Subject.Title,
                        ps.Schedule.AssignedUnits.ToString(),
                        ps.Schedule.OfferCode
                    ))
                    .DistinctBy(s => $"{s.SubjectCode}-{s.SubjectTitle}")
                    .OrderBy(s => s.SubjectCode)
                    .ToList()
                ))
                .OrderByDescending(g => g.HeaderName)
                .ToList();
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

        private ScheduleEvent CreateScheduleEvent(
            string dayKey,
            string subjectCode,
            string title,
            string roomName,
            string units,
            int? offerCode,
            TimeSpan start,
            TimeSpan end)
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
                SubjectCode = subjectCode,
                Title = title,
                StartTimeText = FormatTime(start),
                EndTimeText = FormatTime(end),
                RoomName = roomName,
                Units = units,
                OfferCode = offerCode,
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

            int firstDigit = timeSlot.IndexOfAny("0123456789".ToCharArray());
            string daysPart = firstDigit != -1 ? timeSlot.Substring(0, firstDigit) : timeSlot;

            var upper = daysPart.ToUpper();
            if (upper.Contains("MONDAY")) days.Add("Monday");
            if (upper.Contains("TUESDAY")) days.Add("Tuesday");
            if (upper.Contains("WEDNESDAY")) days.Add("Wednesday");
            if (upper.Contains("THURSDAY")) days.Add("Thursday");
            if (upper.Contains("FRIDAY")) days.Add("Friday");
            if (upper.Contains("SATURDAY")) days.Add("Saturday");

            if (days.Any())
            {
                return OrderDays(days);
            }

            var tokens = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim())
                                 .ToList();

            foreach (var t in tokens)
            {
                if (t.Equals("MWF", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Monday");
                    days.Add("Wednesday");
                    days.Add("Friday");
                }
                else if (t.Equals("TTH", StringComparison.OrdinalIgnoreCase) || t.Equals("TTh", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Tuesday");
                    days.Add("Thursday");
                }
                else if (t.Equals("M", StringComparison.OrdinalIgnoreCase) || t.Equals("Mon", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Monday");
                }
                else if (t.Equals("Th", StringComparison.OrdinalIgnoreCase) || t.Equals("Thu", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Thursday");
                }
                else if (t.Equals("T", StringComparison.OrdinalIgnoreCase) || t.Equals("Tue", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Tuesday");
                }
                else if (t.Equals("W", StringComparison.OrdinalIgnoreCase) || t.Equals("Wed", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Wednesday");
                }
                else if (t.Equals("F", StringComparison.OrdinalIgnoreCase) || t.Equals("Fri", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Friday");
                }
                else if (t.Equals("S", StringComparison.OrdinalIgnoreCase) || t.Equals("Sat", StringComparison.OrdinalIgnoreCase))
                {
                    days.Add("Saturday");
                }
                else
                {
                    var clean = Regex.Replace(t.ToUpper(), @"[^A-Z]", "");
                    if (clean.Contains("TH"))
                    {
                        days.Add("Thursday");
                        clean = clean.Replace("TH", "");
                    }
                    if (clean.Contains("M")) days.Add("Monday");
                    if (clean.Contains("T")) days.Add("Tuesday");
                    if (clean.Contains("W")) days.Add("Wednesday");
                    if (clean.Contains("F")) days.Add("Friday");
                    if (clean.Contains("S")) days.Add("Saturday");
                }
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

        private DateTime ParseStartTime(string timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return DateTime.MaxValue;
            try
            {
                var startPart = timeSlot.Split('-')[0].Trim();
                var timeString = new string(startPart.SkipWhile(c => !char.IsDigit(c)).ToArray());
                if (DateTime.TryParse(timeString, out DateTime parsedTime)) return parsedTime;
            }
            catch { }
            return DateTime.MaxValue;
        }
    }
}
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Admin
{
    public class AuditLogsModel : PageModel
    {
        // 1. Temporary list to store our historical logs
        public static List<LogEntry> Logs { get; set; } = new List<LogEntry>
        {
            new LogEntry { Timestamp = "2026-03-01 09:12", Username = "admin", Action = "Created subject CS101" },
            new LogEntry { Timestamp = "2026-03-02 14:31", Username = "engr.lucky", Action = "Updated room B202" }
        };

        public void OnGet()
        {
            // The page just loads the list, no action needed on load for now
        }
    }

    // A simple class to define what a single log entry looks like
    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
    }
}
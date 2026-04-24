using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FTLSV2.Pages.Admin
{
    public class AuditLogsModel : PageModel
    {
        private readonly FtlsDbContext _context;

        public AuditLogsModel(FtlsDbContext context)
        {
            _context = context;
        }

        // Display data from the view_audit_history view
        public List<AuditLogView> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            // First, fix any NULL user_ids by getting the first admin user
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "admin");
            if (adminUser != null)
            {
                var nullUserLogs = await _context.AuditLogs.Where(a => a.UserId == null).ToListAsync();
                if (nullUserLogs.Any())
                {
                    foreach (var log in nullUserLogs)
                    {
                        log.UserId = adminUser.Id;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Query audit logs and join with users to get user names
            Logs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new AuditLogView
                {
                    Timestamp = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    Username = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : (a.UserId.HasValue ? $"User ID: {a.UserId}" : "Unknown User"),
                    Action = a.Action
                })
                .ToListAsync();
        }
    }

    // View model for displaying audit log data
    public class AuditLogView
    {
        public string Timestamp { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
    }
}
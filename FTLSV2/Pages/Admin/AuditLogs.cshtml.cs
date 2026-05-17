using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
using FTLSV2.Models;
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

        // View model for displaying audit log data
        public List<AuditLogView> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. Fetch all users from the database
            var users = await _context.Users.ToListAsync();

            // 2. Map the user creation data into our AuditLogView list
            Logs = users.Select(u =>
            {
                // Find the name of the person who created this user
                string creatorName = "System / Self-Registered";

                if (u.CreatedBy.HasValue)
                {
                    var creator = users.FirstOrDefault(c => c.Id == u.CreatedBy.Value);
                    creatorName = creator != null
                        ? $"{creator.FirstName} {creator.LastName}".Trim()
                        : $"User ID: {u.CreatedBy}";
                }

                return new AuditLogView
                {
                    // Use the user's creation date as the timestamp
                    Timestamp = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),

                    // The 'Username' column in the table will show who performed the action
                    Username = creatorName,

                    // The 'Action' is dynamically generated
                    Action = $"Created {u.Role} account for {u.FirstName} {u.LastName}"
                };
            })
            // 3. Sort by the newest first
            .OrderByDescending(a => a.Timestamp)
            .ToList();
        }
    }

    // This stays the same so your frontend doesn't break
    public class AuditLogView
    {
        public string Timestamp { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
    }
}
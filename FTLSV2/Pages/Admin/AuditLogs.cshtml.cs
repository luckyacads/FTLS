using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Data;
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

        public List<AuditLogView> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. Fetch raw data from NeonDB and sort it natively using the actual DateTime
            var rawUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.CreatedAt,
                    u.Role,
                    u.FirstName,
                    u.LastName
                })
                .ToListAsync(); 

            // 2. Now that the data is in C#, we can safely use .ToString() formatting
            Logs = rawUsers.Select(u => new AuditLogView
            {
                Timestamp = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Username = "System",
                Action = $"Created {u.Role} account for {u.FirstName} {u.LastName}"
            }).ToList();
        }
    }

    public class AuditLogView
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
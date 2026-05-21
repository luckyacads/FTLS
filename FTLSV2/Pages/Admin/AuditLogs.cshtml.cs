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

        public List<AuditLogView> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            var users = await _context.Users.ToListAsync();

            Logs = users
                .Select(u => new AuditLogView
                {
                    // Now safely using the CreatedAt property from your updated User model
                    Timestamp = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Username = string.IsNullOrWhiteSpace(u.CreatedByRole)
                        ? "System / Self-Registered"
                        : u.CreatedByRole,
                    Action = $"Created {u.Role} account for {u.FirstName} {u.LastName}"
                })
                .OrderByDescending(a => a.Timestamp)
                .ToList();
        }
    }

    public class AuditLogView
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
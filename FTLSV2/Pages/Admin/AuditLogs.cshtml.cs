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
            Logs = await _context.Users
                .Select(u => new AuditLogView
                {
                    Timestamp = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Username = "System",
                    Action = $"Created {u.Role} account for {u.FirstName} {u.LastName}"
                })
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
    }

    public class AuditLogView
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
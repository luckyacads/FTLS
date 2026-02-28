using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class DepartmentReportsModel : PageModel
    {
        public record Report(string Name, string Type, string GeneratedOn);

        public List<Report> Reports { get; set; } = new();

        public void OnGet()
        {
            Reports = new List<Report>
            {
                new Report("Faculty Loads - Fall", "CSV", "2026-03-01"),
                new Report("Room Utilization - March", "PDF", "2026-03-05"),
            };
        }
    }
}

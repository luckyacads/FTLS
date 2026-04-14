using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using FTLSV2.Data;
using FTLSV2.Models;


namespace FTLSV2.Pages.Chairman
{
    public class FacultyProfilesModel : PageModel
    {
        private readonly FtlsDbContext _db;

        public FacultyProfilesModel(FtlsDbContext db)
        {
            _db = db;
        }

        public record FacultyView(string Name, string Email, int CurrentLoad);

        public List<FacultyView> FacultyList { get; set; } = new();

        public void OnGet()
        {
            // Load faculty profiles from the faculty_load_summary view/table
            var summaries = _db.FacultyLoadSummaries.ToList();

            FacultyList = summaries.Select(s =>
                new FacultyView(
                    // preserve existing UI prefix if not present
                    s.Name != null && s.Name.StartsWith("Engr.") ? s.Name : $"Engr. {s.Name}",
                    s.Email,
                    s.CurrentLoad
                )
            ).ToList();
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class FacultyProfilesModel : PageModel
    {
        public record Faculty(string Name, string Email, int CurrentLoad);

        public List<Faculty> FacultyList { get; set; } = new();

        public void OnGet()
        {
            FacultyList = new List<Faculty>
            {
                new Faculty("Engr. Harley", "harley@univ.edu", 12),
                new Faculty("Engr. Lucky", "lucky@univ.edu", 15),
                new Faculty("Engr. Charle", "charle@univ.edu", 6),
            };
        }
    }
}

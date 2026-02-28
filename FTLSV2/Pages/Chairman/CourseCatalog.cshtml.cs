using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages.Chairman
{
    public class CourseCatalogModel : PageModel
    {
        public record Course(string Code, string Title, int Units);

        public List<Course> CourseList { get; set; } = new();

        public void OnGet()
        {
            CourseList = new List<Course>
            {
                new Course("B104","Intro to Programming",3),
                new Course("B105","Data Structures",3),
                new Course("B106","Software Development",3),
            };
        }
    }
}

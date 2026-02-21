using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTLSV2.Pages
{
    public class LoginPageModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // 1. Check if the ID is at least 8 characters long
            if (string.IsNullOrEmpty(Username) || Username.Length < 8)
            {
                ErrorMessage = "Faculty ID must be at least 8 characters long.";
                return Page();
            }
        
            // 1. ADMIN LOGIN -> Goes to AdminPage
            if (Username == "12345678" && Password == "admin1234")
            {
                return RedirectToPage("/AdminPage");
            }

            // 2. TEACHER LOGIN -> Goes to TeacherPage
            // (I made up this ID for Engr. Harley, you can change it)
            else if (Username == "87654321" && Password == "teacher123")
            {
                return RedirectToPage("/TeacherPage");
            }

            // 3. CHAIRMAN LOGIN -> Goes to HomePage
            else if (Username == "11112222" && Password == "chair1234")
            {
                return RedirectToPage("/ChairmanPage");
            }

            // 4. INVALID LOGIN
            else
            {
                ErrorMessage = "Invalid Faculty ID or Password.";
                return Page();
            }
        }
    }
}
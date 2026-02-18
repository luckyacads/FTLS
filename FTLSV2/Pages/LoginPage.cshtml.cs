using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTLSV2.Pages
{
    //This class name matches your HTML "@model" line
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
            if (Username == "admin" && Password == "1234")
            {
                // Redirect to the dashboard
                return RedirectToPage("/HomePage");
            }
            else
            {
                ErrorMessage = "Invalid credentials.";
                return Page();
            }
        }
    }
}
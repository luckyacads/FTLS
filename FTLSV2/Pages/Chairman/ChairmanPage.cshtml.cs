using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTLSV2.Pages.Chairman
{
    public class ChairmanPageModel : PageModel
    {
        // This runs when the page first loads
        public void OnGet()
        {
        }

        // This will run when you click the "Finalize Assignment" button    
        public IActionResult OnPost()
        {
            // Logic for saving assignments goes here
            return Page();
        }
    }
}
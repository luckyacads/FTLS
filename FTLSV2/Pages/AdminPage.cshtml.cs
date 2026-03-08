using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace FTLSV2.Pages
{
    public class AdminPageModel : PageModel
    {
        // --- USER MANAGEMENT DATA ---
        public static List<UserModel> Users { get; set; } = new List<UserModel>
        {
            new UserModel { Name = "Engr. Harley", Role = "Faculty", Status = "Active" },
            new UserModel { Name = "Engr. Lucky", Role = "Faculty", Status = "Inactive" }
        };

        [BindProperty]
        public string NewUserName { get; set; }

        [BindProperty]
        public string NewUserRole { get; set; }

        // --- LOAD CONSTRAINT DATA ---
        // 1. Static variables to store the actual rules
        public static int CurrentRegularLoad { get; set; } = 15;
        public static int CurrentMaxOverload { get; set; } = 21;

        // 2. Bindable properties to catch the numbers typed into the form
        [BindProperty]
        public int InputRegularLoad { get; set; }

        [BindProperty]
        public int InputMaxOverload { get; set; }

        public void OnGet()
        {
            // When the page loads, fill the input boxes with the current saved rules
            InputRegularLoad = CurrentRegularLoad;
            InputMaxOverload = CurrentMaxOverload;
        }

        // --- POST METHODS ---

        public IActionResult OnPostAddUser()
        {
            if (!string.IsNullOrEmpty(NewUserName) && !string.IsNullOrEmpty(NewUserRole))
            {
                Users.Add(new UserModel
                {
                    Name = NewUserName,
                    Role = NewUserRole,
                    Status = "Active"
                });
            }
            return RedirectToPage();
        }

        // 3. This method runs when the "Update Rules" button is clicked
        public IActionResult OnPostUpdateRules()
        {
            // Save the new numbers typed by the user into our "database" variables
            CurrentRegularLoad = InputRegularLoad;
            CurrentMaxOverload = InputMaxOverload;

            // Send a success message to the frontend using TempData
            TempData["SuccessMessage"] = "Load constraint rules successfully updated!";

            return RedirectToPage();
        }

        // 4. This method runs when the "Activate" or "Deactivate" button is clicked
        public IActionResult OnPostToggleUserStatus(string userName)
        {
            // Find the specific user in our list based on the name passed from HTML
            var userToUpdate = Users.Find(u => u.Name == userName);

            if (userToUpdate != null)
            {
                // Flip the status
                if (userToUpdate.Status == "Active")
                {
                    userToUpdate.Status = "Inactive";
                }
                else
                {
                    userToUpdate.Status = "Active";
                }

                // Show a quick success message
                TempData["SuccessMessage"] = $"{userToUpdate.Name} is now {userToUpdate.Status}.";
            }

            return RedirectToPage();
        }
    }

    public class UserModel
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
    }
}
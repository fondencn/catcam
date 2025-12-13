using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CatCam.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect authenticated users to webcam, others to login
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Webcam");
        }
        
        return RedirectToPage("/Login");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CatCam.Web.Pages
{
    [Authorize]
    public class WebcamModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

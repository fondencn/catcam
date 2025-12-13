using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CatCam.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            // If already authenticated, redirect to webcam
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Webcam");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var configuredPassword = _configuration["Authentication:Password"];

            if (string.IsNullOrEmpty(configuredPassword))
            {
                ErrorMessage = "Authentication is not properly configured.";
                return Page();
            }

            if (Password == configuredPassword)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "CatCam User"),
                    new Claim(ClaimTypes.Role, "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToPage("/Webcam");
            }

            ErrorMessage = "Invalid password. Please try again.";
            return Page();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Web.Services.ApiClients;

namespace TechMove.GLMS.Web.Controllers
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email / Username is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "admin@techmove.com";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "Admin@123";

        public string? ReturnUrl { get; set; }
    }

    public class AccountController : Controller
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthApiClient authApiClient, ILogger<AccountController> logger)
        {
            _authApiClient = authApiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var authResult = await _authApiClient.LoginAsync(model.Email, model.Password);
                if (authResult == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials or Backend Web API refused authentication.");
                    return View(model);
                }

                TempData["SuccessMessage"] = $"Welcome, {authResult.FullName}! Authenticated with JWT Bearer Token (Role: {authResult.Role}).";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error during authentication against API");
                ModelState.AddModelError(string.Empty, $"Authentication Error: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            _authApiClient.Logout();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}


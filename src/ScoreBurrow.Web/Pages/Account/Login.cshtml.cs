using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScoreBurrow.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace ScoreBurrow.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = "";

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("Login POST handler called for {Email}", Input?.Email ?? "null");
            
            returnUrl ??= Url.Content("~/");
            
            _logger.LogInformation("ReturnUrl: {ReturnUrl}", returnUrl);

            if (ModelState.IsValid && Input != null)
            {
                _logger.LogInformation("ModelState is valid, attempting sign in for {Email}", Input.Email!);
                
                var result = await _signInManager.PasswordSignInAsync(Input.Email!, Input.Password!, Input.RememberMe, lockoutOnFailure: false);

                _logger.LogInformation("Sign in result: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, RequiresTwoFactor={RequiresTwoFactor}", 
                    result.Succeeded, result.IsLockedOut, result.RequiresTwoFactor);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Login successful, redirecting to {ReturnUrl}", returnUrl);
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    _logger.LogWarning("Login failed for {Email}", Input.Email!);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            return Page();
        }
    }
}

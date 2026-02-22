using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LostAndFoundApp.Models;
using LostAndFoundApp.Services;
using LostAndFoundApp.ViewModels;

namespace LostAndFoundApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AdSyncService _adSyncService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AdSyncService adSyncService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _adSyncService = adSyncService;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Contact an administrator.");
                return View(model);
            }

            if (user.IsAdUser)
            {
                // AD user: validate credentials against Active Directory — never stored locally
                var adValid = _adSyncService.ValidateAdCredentials(model.UserName, model.Password);
                if (!adValid)
                {
                    ModelState.AddModelError(string.Empty, "Invalid Active Directory credentials.");
                    _logger.LogWarning("AD login failed for user '{User}'.", model.UserName);
                    return View(model);
                }

                // Sign in using Identity cookie with the AD user's local profile and role
                await _signInManager.SignInAsync(user, model.RememberMe);
                _logger.LogInformation("AD user '{User}' logged in successfully.", model.UserName);
                return RedirectToLocal(model.ReturnUrl);
            }
            else
            {
                // Local user: standard Identity credential validation with lockout
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Local user '{User}' logged in successfully.", model.UserName);

                    // MustChangePassword redirect is handled by middleware
                    return RedirectToLocal(model.ReturnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User '{User}' account locked out.", model.UserName);
                    ModelState.AddModelError(string.Empty, "Account locked out due to too many failed attempts. Try again later.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // AD users do not change passwords through this app
            if (user.IsAdUser)
            {
                TempData["ErrorMessage"] = "Active Directory users must change their password through their organization's password management system.";
                return RedirectToAction("Index", "Home");
            }

            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (user.IsAdUser)
            {
                TempData["ErrorMessage"] = "Active Directory users cannot change passwords here.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Clear the MustChangePassword flag after successful password change
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("User '{User}' changed their password.", user.UserName);
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}

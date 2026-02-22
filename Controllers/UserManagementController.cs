using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Data;
using LostAndFoundApp.Models;
using LostAndFoundApp.Services;
using LostAndFoundApp.ViewModels;

namespace LostAndFoundApp.Controllers
{
    /// <summary>
    /// User management and AD sync — accessible only to Super Admin.
    /// Handles user listing, local user creation, role assignment, activation/deactivation,
    /// AD group management, and AD sync triggering.
    /// </summary>
    [Authorize(Policy = "RequireSuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly AdSyncService _adSyncService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            AdSyncService adSyncService,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _adSyncService = adSyncService;
            _logger = logger;
        }

        // GET: /UserManagement
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "None",
                    AccountType = user.IsAdUser ? "Active Directory" : "Local",
                    IsActive = user.IsActive
                });
            }

            return View(userList.OrderBy(u => u.UserName).ToList());
        }

        // GET: /UserManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _userManager.FindByNameAsync(model.UserName);
            if (existing != null)
            {
                ModelState.AddModelError("UserName", "A user with this username already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                DisplayName = model.DisplayName,
                EmailConfirmed = true,
                IsAdUser = false,
                MustChangePassword = true, // Local users must change password on first login
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Assign role
            if (!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            _logger.LogInformation("Super Admin created local user '{User}' with role '{Role}'.", model.UserName, model.Role);
            TempData["SuccessMessage"] = $"User '{model.UserName}' created successfully with role '{model.Role}'. They must change their password on first login.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /UserManagement/EditRole/userId
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
                DisplayName = user.DisplayName,
                CurrentRole = roles.FirstOrDefault() ?? "None",
                NewRole = roles.FirstOrDefault() ?? "User"
            };

            return View(model);
        }

        // POST: /UserManagement/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditUserRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Remove all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add new role
            await _userManager.AddToRoleAsync(user, model.NewRole);

            _logger.LogInformation("Role for user '{User}' changed from '{OldRole}' to '{NewRole}'.",
                user.UserName, model.CurrentRole, model.NewRole);
            TempData["SuccessMessage"] = $"Role for '{user.UserName}' changed to '{model.NewRole}'.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserManagement/ToggleActive/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deactivating yourself
            if (user.UserName == User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var status = user.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("User '{User}' has been {Status}.", user.UserName, status);
            TempData["SuccessMessage"] = $"User '{user.UserName}' has been {status}.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // ACTIVE DIRECTORY GROUP MANAGEMENT
        // =====================================================================

        // GET: /UserManagement/AdGroups
        public async Task<IActionResult> AdGroups()
        {
            var groups = await _context.AdGroups.OrderBy(g => g.GroupName).ToListAsync();
            return View(groups);
        }

        // POST: /UserManagement/AddAdGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                TempData["ErrorMessage"] = "Group name is required.";
                return RedirectToAction(nameof(AdGroups));
            }

            var trimmed = groupName.Trim();
            if (await _context.AdGroups.AnyAsync(g => g.GroupName == trimmed))
            {
                TempData["ErrorMessage"] = $"AD group '{trimmed}' is already configured.";
                return RedirectToAction(nameof(AdGroups));
            }

            _context.AdGroups.Add(new AdGroup { GroupName = trimmed, DateAdded = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            _logger.LogInformation("AD group '{GroupName}' added by '{User}'.", trimmed, User.Identity?.Name);
            TempData["SuccessMessage"] = $"AD group '{trimmed}' added successfully.";
            return RedirectToAction(nameof(AdGroups));
        }

        // POST: /UserManagement/RemoveAdGroup/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdGroup(int id)
        {
            var group = await _context.AdGroups.FindAsync(id);
            if (group == null) return NotFound();

            _context.AdGroups.Remove(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AD group '{GroupName}' removed by '{User}'.", group.GroupName, User.Identity?.Name);
            TempData["SuccessMessage"] = $"AD group '{group.GroupName}' removed.";
            return RedirectToAction(nameof(AdGroups));
        }

        // POST: /UserManagement/SyncNow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncNow()
        {
            var result = await _adSyncService.SyncUsersAsync();

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Summary;
            }
            else
            {
                TempData["ErrorMessage"] = result.Summary;
            }

            _logger.LogInformation("AD sync triggered by '{User}': {Summary}", User.Identity?.Name, result.Summary);
            return RedirectToAction(nameof(AdGroups));
        }
    }
}

using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Data;
using LostAndFoundApp.Models;

namespace LostAndFoundApp.Services
{
    /// <summary>
    /// Service responsible for synchronizing users from configured Active Directory groups
    /// into the local application database. Also handles runtime AD credential validation.
    /// </summary>
    public class AdSyncService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdSyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AdSyncService(IConfiguration config, ILogger<AdSyncService> logger, IServiceScopeFactory scopeFactory)
        {
            _config = config;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Validates user credentials against Active Directory in real time.
        /// Returns true if the credentials are valid, false otherwise.
        /// AD credentials are never stored locally.
        /// </summary>
        public bool ValidateAdCredentials(string username, string password)
        {
            try
            {
                var domain = _config["ActiveDirectory:Domain"];
                var container = _config["ActiveDirectory:Container"];
                var useSsl = _config.GetValue<bool>("ActiveDirectory:UseSSL", false);

                if (string.IsNullOrEmpty(domain))
                {
                    _logger.LogError("Active Directory domain is not configured in appsettings.");
                    return false;
                }

                var options = useSsl ? ContextOptions.Negotiate | ContextOptions.SecureSocketLayer : ContextOptions.Negotiate;
                using var context = new PrincipalContext(ContextType.Domain, domain, container, options);
                
                var isValid = context.ValidateCredentials(username, password);
                _logger.LogInformation("AD credential validation for user '{User}': {Result}", username, isValid ? "Success" : "Failed");
                return isValid;
            }
            catch (PrincipalServerDownException ex)
            {
                _logger.LogError(ex, "Active Directory server is not reachable for credential validation.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating AD credentials for user '{User}'.", username);
                return false;
            }
        }

        /// <summary>
        /// Synchronizes users from all configured AD groups into the local database.
        /// New members are created with IsAdUser=true. Members no longer in any group are flagged inactive.
        /// Returns a summary of the sync operation.
        /// </summary>
        public async Task<AdSyncResult> SyncUsersAsync()
        {
            var result = new AdSyncResult();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var domain = _config["ActiveDirectory:Domain"];
                var container = _config["ActiveDirectory:Container"];
                var useSsl = _config.GetValue<bool>("ActiveDirectory:UseSSL", false);

                if (string.IsNullOrEmpty(domain))
                {
                    result.Errors.Add("Active Directory domain is not configured.");
                    return result;
                }

                var adGroups = await context.AdGroups.ToListAsync();
                if (!adGroups.Any())
                {
                    result.Errors.Add("No AD groups configured for synchronization.");
                    return result;
                }

                var syncedSamAccountNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var options = useSsl ? ContextOptions.Negotiate | ContextOptions.SecureSocketLayer : ContextOptions.Negotiate;
                using var principalContext = new PrincipalContext(ContextType.Domain, domain, container, options);

                foreach (var adGroup in adGroups)
                {
                    try
                    {
                        using var group = GroupPrincipal.FindByIdentity(principalContext, adGroup.GroupName);
                        if (group == null)
                        {
                            result.Errors.Add($"AD group '{adGroup.GroupName}' not found in directory.");
                            continue;
                        }

                        using var members = group.GetMembers(true);
                        foreach (var member in members)
                        {
                            using (member) // Dispose each Principal after use
                            {
                            if (member is UserPrincipal userPrincipal)
                            {
                                var samAccountName = userPrincipal.SamAccountName;
                                if (string.IsNullOrEmpty(samAccountName))
                                    continue;

                                syncedSamAccountNames.Add(samAccountName);

                                var existingUser = await userManager.FindByNameAsync(samAccountName);
                                if (existingUser == null)
                                {
                                    // Create new AD-synced user — no local password, no MustChangePassword
                                    var newUser = new ApplicationUser
                                    {
                                        UserName = samAccountName,
                                        Email = userPrincipal.EmailAddress ?? $"{samAccountName}@{domain}",
                                        EmailConfirmed = true,
                                        DisplayName = userPrincipal.DisplayName ?? samAccountName,
                                        IsAdUser = true,
                                        MustChangePassword = false,
                                        IsActive = true,
                                        SamAccountName = samAccountName
                                    };

                                    // AD users are created without a password — they authenticate against AD
                                    var createResult = await userManager.CreateAsync(newUser);
                                    if (createResult.Succeeded)
                                    {
                                        // Default role for new AD users is "User"
                                        await userManager.AddToRoleAsync(newUser, "User");
                                        result.UsersCreated++;
                                    }
                                    else
                                    {
                                        result.Errors.Add($"Failed to create user '{samAccountName}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                                    }
                                }
                                else
                                {
                                    // Update existing user's display name and email from AD
                                    bool updated = false;
                                    if (existingUser.DisplayName != userPrincipal.DisplayName && !string.IsNullOrEmpty(userPrincipal.DisplayName))
                                    {
                                        existingUser.DisplayName = userPrincipal.DisplayName;
                                        updated = true;
                                    }
                                    if (existingUser.Email != userPrincipal.EmailAddress && !string.IsNullOrEmpty(userPrincipal.EmailAddress))
                                    {
                                        existingUser.Email = userPrincipal.EmailAddress;
                                        updated = true;
                                    }
                                    // Reactivate if previously deactivated
                                    if (!existingUser.IsActive)
                                    {
                                        existingUser.IsActive = true;
                                        updated = true;
                                    }
                                    if (updated)
                                    {
                                        await userManager.UpdateAsync(existingUser);
                                        result.UsersUpdated++;
                                    }
                                }
                            }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error processing AD group '{adGroup.GroupName}': {ex.Message}");
                        _logger.LogError(ex, "Error processing AD group '{GroupName}'", adGroup.GroupName);
                    }
                }

                // Flag AD users no longer in any configured group as inactive (do not delete)
                var allAdUsers = await context.Users
                    .Where(u => u.IsAdUser && u.IsActive)
                    .ToListAsync();

                foreach (var adUser in allAdUsers)
                {
                    if (!string.IsNullOrEmpty(adUser.SamAccountName) &&
                        !syncedSamAccountNames.Contains(adUser.SamAccountName))
                    {
                        adUser.IsActive = false;
                        result.UsersDeactivated++;
                    }
                }

                await context.SaveChangesAsync();
                result.Success = true;
                _logger.LogInformation("AD sync completed. Created: {Created}, Updated: {Updated}, Deactivated: {Deactivated}",
                    result.UsersCreated, result.UsersUpdated, result.UsersDeactivated);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"AD sync failed: {ex.Message}");
                _logger.LogError(ex, "AD sync failed with unexpected error.");
            }

            return result;
        }
    }

    /// <summary>
    /// Result object for AD synchronization operations
    /// </summary>
    public class AdSyncResult
    {
        public bool Success { get; set; }
        public int UsersCreated { get; set; }
        public int UsersUpdated { get; set; }
        public int UsersDeactivated { get; set; }
        public List<string> Errors { get; set; } = new();

        public string Summary =>
            $"Sync {(Success ? "completed" : "failed")}. " +
            $"Created: {UsersCreated}, Updated: {UsersUpdated}, Deactivated: {UsersDeactivated}." +
            (Errors.Any() ? $" Errors: {string.Join("; ", Errors)}" : "");
    }
}

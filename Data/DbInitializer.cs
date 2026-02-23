using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LostAndFoundApp.Models;

namespace LostAndFoundApp.Data
{
    /// <summary>
    /// Seeds the database with roles, default accounts for all three roles,
    /// and default master data on first run.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            // ─── Seed Roles ────────────────────────────────────────
            string[] roles = { "SuperAdmin", "Supervisor", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ─── Seed Default Accounts ─────────────────────────────
            // Account 1: Super Admin — full system control
            await SeedUserAsync(userManager, logger, new SeedAccount
            {
                UserName = "sadmin",
                Email = "sadmin@lostandfound.local",
                DisplayName = "System Administrator",
                Password = "Rider@2025",
                Role = "SuperAdmin",
                MustChangePassword = true
            });

            // Account 2: Supervisor — operational management
            await SeedUserAsync(userManager, logger, new SeedAccount
            {
                UserName = "supervisor",
                Email = "supervisor@lostandfound.local",
                DisplayName = "Shift Supervisor",
                Password = "Rider@2025",
                Role = "Supervisor",
                MustChangePassword = true
            });

            // Account 3: User — standard data entry operator
            await SeedUserAsync(userManager, logger, new SeedAccount
            {
                UserName = "user",
                Email = "user@lostandfound.local",
                DisplayName = "Staff User",
                Password = "Rider@2025",
                Role = "User",
                MustChangePassword = true
            });

            // ─── Seed Default Status Values ────────────────────────
            if (!await context.Statuses.AnyAsync())
            {
                context.Statuses.AddRange(
                    new Status { Name = "Found" },
                    new Status { Name = "Claimed" },
                    new Status { Name = "Disposed" },
                    new Status { Name = "Stored" },
                    new Status { Name = "Transferred" }
                );
                await context.SaveChangesAsync();
            }

            // ─── Seed Default Item Types ───────────────────────────
            if (!await context.Items.AnyAsync())
            {
                context.Items.AddRange(
                    new Item { Name = "Wallet" },
                    new Item { Name = "Phone" },
                    new Item { Name = "Keys" },
                    new Item { Name = "Bag" },
                    new Item { Name = "Umbrella" },
                    new Item { Name = "Clothing" },
                    new Item { Name = "Electronics" },
                    new Item { Name = "Jewelry" },
                    new Item { Name = "Documents" },
                    new Item { Name = "Other" }
                );
                await context.SaveChangesAsync();
            }

            // ─── Seed Default Storage Locations ────────────────────
            if (!await context.StorageLocations.AnyAsync())
            {
                context.StorageLocations.AddRange(
                    new StorageLocation { Name = "Main Office" },
                    new StorageLocation { Name = "Storage Room A" },
                    new StorageLocation { Name = "Storage Room B" },
                    new StorageLocation { Name = "Security Desk" }
                );
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Creates a user account if it does not already exist, then assigns the specified role.
        /// </summary>
        private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, ILogger logger, SeedAccount account)
        {
            var existing = await userManager.FindByNameAsync(account.UserName);
            if (existing != null) return;

            var user = new ApplicationUser
            {
                UserName = account.UserName,
                Email = account.Email,
                EmailConfirmed = true,
                DisplayName = account.DisplayName,
                IsAdUser = false,
                MustChangePassword = account.MustChangePassword,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, account.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, account.Role);
                logger.LogInformation("Seeded user '{UserName}' with role '{Role}'.", account.UserName, account.Role);
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to seed user '{UserName}': {Errors}", account.UserName, errors);
            }
        }

        /// <summary>Helper class for seeding user accounts.</summary>
        private class SeedAccount
        {
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool MustChangePassword { get; set; }
        }
    }
}

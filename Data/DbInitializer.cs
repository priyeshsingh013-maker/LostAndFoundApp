using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Models;

namespace LostAndFoundApp.Data
{
    /// <summary>
    /// Seeds the database with roles, default super admin account, and default master data on first run.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply any pending migrations automatically on startup
            await context.Database.MigrateAsync();

            // Seed roles
            string[] roles = { "SuperAdmin", "Supervisor", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed default Super Admin account
            const string adminUserName = "sadmin";
            const string adminPassword = "Rider@2025";
            const string adminEmail = "sadmin@lostandfound.local";

            var existingAdmin = await userManager.FindByNameAsync(adminUserName);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "System Administrator",
                    IsAdUser = false,
                    MustChangePassword = true, // Must change password on first login
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                }
            }

            // Seed default Status values
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

            // Seed default Item types
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

            // Seed default Storage Locations
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
    }
}

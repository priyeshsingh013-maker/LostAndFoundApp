using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Models;

namespace LostAndFoundApp.Data
{
    /// <summary>
    /// Application database context inheriting from IdentityDbContext for ASP.NET Core Identity tables,
    /// extended with all application-specific entities.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Master data tables
        public DbSet<Item> Items { get; set; }
        public DbSet<Models.Route> Routes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<StorageLocation> StorageLocations { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<FoundByName> FoundByNames { get; set; }

        // Primary tracking table
        public DbSet<LostFoundItem> LostFoundItems { get; set; }

        // AD groups table
        public DbSet<AdGroup> AdGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // LostFoundItem relationships and constraints configured via Fluent API
            builder.Entity<LostFoundItem>(entity =>
            {
                entity.HasKey(e => e.TrackingId);

                entity.HasOne(e => e.Item)
                    .WithMany()
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Route)
                    .WithMany()
                    .HasForeignKey(e => e.RouteId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.StorageLocation)
                    .WithMany()
                    .HasForeignKey(e => e.StorageLocationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Status)
                    .WithMany()
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FoundBy)
                    .WithMany()
                    .HasForeignKey(e => e.FoundById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.DateFound).IsRequired();
                entity.Property(e => e.LocationFound).IsRequired().HasMaxLength(300);
                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Index on DateFound for efficient range queries in search
                entity.HasIndex(e => e.DateFound);
                entity.HasIndex(e => e.StatusId);
            });

            // Master table configurations
            builder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<Models.Route>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<StorageLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<Status>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<FoundByName>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<AdGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GroupName).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.GroupName).IsUnique();
                entity.Property(e => e.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}

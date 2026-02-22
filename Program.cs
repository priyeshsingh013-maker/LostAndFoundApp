using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Data;
using LostAndFoundApp.Models;
using LostAndFoundApp.Services;
using LostAndFoundApp.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog Configuration ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// --- Database ---
// Detect if Render.com injected the POSTGRES_URL environment variable, otherwise fallback to local connection string.
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- ASP.NET Core Identity ---
var lockoutAttempts = builder.Configuration.GetValue<int>("Identity:MaxFailedAccessAttempts", 5);
var lockoutMinutes = builder.Configuration.GetValue<int>("Identity:LockoutMinutes", 15);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout policy — configurable via appsettings
    options.Lockout.MaxFailedAccessAttempts = lockoutAttempts;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = false; // AD users may share email patterns
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- Cookie configuration ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// --- Authorization policies for role-based access control ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("RequireSupervisorOrAbove", policy => policy.RequireRole("SuperAdmin", "Supervisor"));
    options.AddPolicy("RequireAnyRole", policy => policy.RequireRole("SuperAdmin", "Supervisor", "User"));
});

// --- Application services ---
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<AdSyncService>();

// --- Configure antiforgery to accept token from AJAX header ---
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// --- MVC ---
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Apply Migrations on Startup ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

// --- Seed database on startup ---
if (Environment.GetEnvironmentVariable("SEED_DATABASE") == "true" || app.Environment.IsDevelopment())
{
    await DbInitializer.SeedAsync(app.Services);
}

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}
// --- Forwarded headers for reverse proxy (Render.com) ---
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MustChangePassword middleware runs after auth so we know who the user is
app.UseMustChangePassword();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

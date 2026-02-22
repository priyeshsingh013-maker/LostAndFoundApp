using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Data;
using LostAndFoundApp.Models;
using LostAndFoundApp.ViewModels;
using System.Diagnostics;

namespace LostAndFoundApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalItems = await _context.LostFoundItems.CountAsync(),
                FoundCount = await _context.LostFoundItems
                    .Include(x => x.Status)
                    .CountAsync(x => x.Status != null && x.Status.Name == "Found"),
                ClaimedCount = await _context.LostFoundItems
                    .Include(x => x.Status)
                    .CountAsync(x => x.Status != null && x.Status.Name == "Claimed"),
                StoredCount = await _context.LostFoundItems
                    .Include(x => x.Status)
                    .CountAsync(x => x.Status != null && x.Status.Name == "Stored"),
            };

            vm.RecentRecords = await _context.LostFoundItems
                .Include(x => x.Item)
                .Include(x => x.Status)
                .OrderByDescending(x => x.CreatedDateTime)
                .Take(10)
                .Select(x => new DashboardRecentItem
                {
                    TrackingId = x.TrackingId,
                    DateFound = x.DateFound,
                    ItemName = x.Item != null ? x.Item.Name : "",
                    LocationFound = x.LocationFound,
                    StatusName = x.Status != null ? x.Status.Name : "",
                    DaysSinceFound = (DateTime.Today - x.DateFound.Date).Days
                })
                .ToListAsync();

            return View(vm);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

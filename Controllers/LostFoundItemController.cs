using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LostAndFoundApp.Data;
using LostAndFoundApp.Models;
using LostAndFoundApp.Services;
using LostAndFoundApp.ViewModels;

namespace LostAndFoundApp.Controllers
{
    [Authorize]
    public class LostFoundItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;
        private readonly ILogger<LostFoundItemController> _logger;

        public LostFoundItemController(ApplicationDbContext context, FileService fileService, ILogger<LostFoundItemController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        // GET: /LostFoundItem/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new LostFoundItemCreateViewModel
            {
                DateFound = DateTime.Today,
                StatusDate = DateTime.Today
            };
            await PopulateDropdowns(vm);
            return View(vm);
        }

        // POST: /LostFoundItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LostFoundItemCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm);
                return View(vm);
            }

            var item = new LostFoundItem
            {
                DateFound = vm.DateFound,
                ItemId = vm.ItemId,
                Description = vm.Description,
                LocationFound = vm.LocationFound,
                RouteId = vm.RouteId,
                VehicleId = vm.VehicleId,
                StorageLocationId = vm.StorageLocationId,
                StatusId = vm.StatusId,
                StatusDate = vm.StatusDate,
                FoundById = vm.FoundById,
                ClaimedBy = vm.ClaimedBy,
                Notes = vm.Notes,
                CreatedBy = User.Identity?.Name ?? "Unknown",
                CreatedDateTime = DateTime.UtcNow
            };

            // Handle photo upload
            if (vm.PhotoFile != null && vm.PhotoFile.Length > 0)
            {
                var photoName = await _fileService.SavePhotoAsync(vm.PhotoFile);
                if (photoName == null)
                {
                    ModelState.AddModelError("PhotoFile", "Invalid photo file. Allowed types: jpg, jpeg, png, gif. Max size: 10MB.");
                    await PopulateDropdowns(vm);
                    return View(vm);
                }
                item.PhotoPath = photoName;
            }

            // Handle attachment upload
            if (vm.AttachmentFile != null && vm.AttachmentFile.Length > 0)
            {
                var attachmentName = await _fileService.SaveAttachmentAsync(vm.AttachmentFile);
                if (attachmentName == null)
                {
                    ModelState.AddModelError("AttachmentFile", "Invalid attachment file. Allowed types: pdf, doc, docx, xls, xlsx, txt, jpg, jpeg, png. Max size: 10MB.");
                    await PopulateDropdowns(vm);
                    return View(vm);
                }
                item.AttachmentPath = attachmentName;
            }

            _context.LostFoundItems.Add(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("LostFoundItem {TrackingId} created by {User}", item.TrackingId, item.CreatedBy);
            TempData["SuccessMessage"] = $"Item record #{item.TrackingId} created successfully.";
            return RedirectToAction(nameof(Details), new { id = item.TrackingId });
        }

        // GET: /LostFoundItem/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.LostFoundItems
                .Include(x => x.Item)
                .Include(x => x.Route)
                .Include(x => x.Vehicle)
                .Include(x => x.StorageLocation)
                .Include(x => x.Status)
                .Include(x => x.FoundBy)
                .FirstOrDefaultAsync(x => x.TrackingId == id);

            if (item == null)
                return NotFound();

            var vm = new LostFoundItemDetailViewModel
            {
                TrackingId = item.TrackingId,
                DateFound = item.DateFound,
                ItemName = item.Item?.Name,
                Description = item.Description,
                LocationFound = item.LocationFound,
                RouteName = item.Route?.Name,
                VehicleName = item.Vehicle?.Name,
                PhotoPath = item.PhotoPath,
                StorageLocationName = item.StorageLocation?.Name,
                StatusName = item.Status?.Name,
                StatusDate = item.StatusDate,
                DaysSinceFound = item.DaysSinceFound,
                FoundByName = item.FoundBy?.Name,
                ClaimedBy = item.ClaimedBy,
                CreatedBy = item.CreatedBy,
                CreatedDateTime = item.CreatedDateTime,
                Notes = item.Notes,
                AttachmentPath = item.AttachmentPath
            };

            return View(vm);
        }

        // GET: /LostFoundItem/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.LostFoundItems.FindAsync(id);
            if (item == null)
                return NotFound();

            var vm = new LostFoundItemEditViewModel
            {
                TrackingId = item.TrackingId,
                DateFound = item.DateFound,
                ItemId = item.ItemId,
                Description = item.Description,
                LocationFound = item.LocationFound,
                RouteId = item.RouteId,
                VehicleId = item.VehicleId,
                StorageLocationId = item.StorageLocationId,
                StatusId = item.StatusId,
                StatusDate = item.StatusDate,
                FoundById = item.FoundById,
                ClaimedBy = item.ClaimedBy,
                Notes = item.Notes,
                ExistingPhotoPath = item.PhotoPath,
                ExistingAttachmentPath = item.AttachmentPath,
                CreatedBy = item.CreatedBy,
                CreatedDateTime = item.CreatedDateTime
            };

            await PopulateDropdowns(vm);
            return View(vm);
        }

        // POST: /LostFoundItem/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LostFoundItemEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm);
                return View(vm);
            }

            var item = await _context.LostFoundItems.FindAsync(vm.TrackingId);
            if (item == null)
                return NotFound();

            item.DateFound = vm.DateFound;
            item.ItemId = vm.ItemId;
            item.Description = vm.Description;
            item.LocationFound = vm.LocationFound;
            item.RouteId = vm.RouteId;
            item.VehicleId = vm.VehicleId;
            item.StorageLocationId = vm.StorageLocationId;
            item.StatusId = vm.StatusId;
            item.StatusDate = vm.StatusDate;
            item.FoundById = vm.FoundById;
            item.ClaimedBy = vm.ClaimedBy;
            item.Notes = vm.Notes;
            // CreatedBy and CreatedDateTime are never modified

            // Handle photo replacement
            if (vm.PhotoFile != null && vm.PhotoFile.Length > 0)
            {
                var photoName = await _fileService.SavePhotoAsync(vm.PhotoFile);
                if (photoName == null)
                {
                    ModelState.AddModelError("PhotoFile", "Invalid photo file. Allowed types: jpg, jpeg, png, gif. Max size: 10MB.");
                    await PopulateDropdowns(vm);
                    return View(vm);
                }
                // Delete old photo
                _fileService.DeletePhoto(item.PhotoPath);
                item.PhotoPath = photoName;
            }

            // Handle attachment replacement
            if (vm.AttachmentFile != null && vm.AttachmentFile.Length > 0)
            {
                var attachmentName = await _fileService.SaveAttachmentAsync(vm.AttachmentFile);
                if (attachmentName == null)
                {
                    ModelState.AddModelError("AttachmentFile", "Invalid attachment file.");
                    await PopulateDropdowns(vm);
                    return View(vm);
                }
                _fileService.DeleteAttachment(item.AttachmentPath);
                item.AttachmentPath = attachmentName;
            }

            _context.LostFoundItems.Update(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("LostFoundItem {TrackingId} updated by {User}", item.TrackingId, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Item record #{item.TrackingId} updated successfully.";
            return RedirectToAction(nameof(Details), new { id = item.TrackingId });
        }

        // GET: /LostFoundItem/Search
        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel? vm)
        {
            vm ??= new SearchViewModel();

            await PopulateSearchDropdowns(vm);

            // Build query
            var query = _context.LostFoundItems.AsQueryable();

            // Build filter summary for print
            var filters = new List<string>();

            // Apply filters — empty/null filters are ignored
            if (vm.TrackingId.HasValue)
            {
                query = query.Where(x => x.TrackingId == vm.TrackingId.Value);
                filters.Add($"Tracking ID: {vm.TrackingId.Value}");
            }
            if (vm.DateFoundFrom.HasValue)
            {
                query = query.Where(x => x.DateFound >= vm.DateFoundFrom.Value);
                filters.Add($"Date Found From: {vm.DateFoundFrom.Value:MM/dd/yyyy}");
            }
            if (vm.DateFoundTo.HasValue)
            {
                query = query.Where(x => x.DateFound <= vm.DateFoundTo.Value);
                filters.Add($"Date Found To: {vm.DateFoundTo.Value:MM/dd/yyyy}");
            }
            if (vm.ItemId.HasValue)
            {
                query = query.Where(x => x.ItemId == vm.ItemId.Value);
                var itemName = vm.Items?.FirstOrDefault(i => i.Value == vm.ItemId.Value.ToString())?.Text ?? vm.ItemId.Value.ToString();
                filters.Add($"Item: {itemName}");
            }
            if (vm.StatusId.HasValue)
            {
                query = query.Where(x => x.StatusId == vm.StatusId.Value);
                var statusName = vm.Statuses?.FirstOrDefault(i => i.Value == vm.StatusId.Value.ToString())?.Text ?? vm.StatusId.Value.ToString();
                filters.Add($"Status: {statusName}");
            }
            if (vm.RouteId.HasValue)
            {
                query = query.Where(x => x.RouteId == vm.RouteId.Value);
                var routeName = vm.Routes?.FirstOrDefault(i => i.Value == vm.RouteId.Value.ToString())?.Text ?? vm.RouteId.Value.ToString();
                filters.Add($"Route #: {routeName}");
            }
            if (vm.VehicleId.HasValue)
            {
                query = query.Where(x => x.VehicleId == vm.VehicleId.Value);
                var vehicleName = vm.Vehicles?.FirstOrDefault(i => i.Value == vm.VehicleId.Value.ToString())?.Text ?? vm.VehicleId.Value.ToString();
                filters.Add($"Vehicle #: {vehicleName}");
            }
            if (vm.StorageLocationId.HasValue)
            {
                query = query.Where(x => x.StorageLocationId == vm.StorageLocationId.Value);
                var locName = vm.StorageLocations?.FirstOrDefault(i => i.Value == vm.StorageLocationId.Value.ToString())?.Text ?? vm.StorageLocationId.Value.ToString();
                filters.Add($"Storage Location: {locName}");
            }
            if (vm.FoundById.HasValue)
            {
                query = query.Where(x => x.FoundById == vm.FoundById.Value);
                var foundName = vm.FoundByNames?.FirstOrDefault(i => i.Value == vm.FoundById.Value.ToString())?.Text ?? vm.FoundById.Value.ToString();
                filters.Add($"Found By: {foundName}");
            }

            vm.FilterSummary = filters.Any() ? string.Join(" | ", filters) : "All Records (No Filters Applied)";

            // Sort
            query = vm.SortField switch
            {
                "DateFound" => vm.SortOrder == "asc" ? query.OrderBy(x => x.DateFound) : query.OrderByDescending(x => x.DateFound),
                "ItemName" => vm.SortOrder == "asc" ? query.OrderBy(x => x.Item!.Name) : query.OrderByDescending(x => x.Item!.Name),
                "StatusName" => vm.SortOrder == "asc" ? query.OrderBy(x => x.Status!.Name) : query.OrderByDescending(x => x.Status!.Name),
                "LocationFound" => vm.SortOrder == "asc" ? query.OrderBy(x => x.LocationFound) : query.OrderByDescending(x => x.LocationFound),
                _ => vm.SortOrder == "asc" ? query.OrderBy(x => x.TrackingId) : query.OrderByDescending(x => x.TrackingId),
            };

            // Pagination
            vm.TotalRecords = await query.CountAsync();
            vm.TotalPages = (int)Math.Ceiling((double)vm.TotalRecords / vm.PageSize);
            if (vm.CurrentPage < 1) vm.CurrentPage = 1;
            if (vm.CurrentPage > vm.TotalPages && vm.TotalPages > 0) vm.CurrentPage = vm.TotalPages;

            var rawResults = await query
                .Skip((vm.CurrentPage - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .Select(x => new
                {
                    x.TrackingId,
                    x.DateFound,
                    ItemName = x.Item != null ? x.Item.Name : "",
                    x.Description,
                    x.LocationFound,
                    RouteName = x.Route != null ? x.Route.Name : "",
                    VehicleName = x.Vehicle != null ? x.Vehicle.Name : "",
                    StorageLocationName = x.StorageLocation != null ? x.StorageLocation.Name : "",
                    StatusName = x.Status != null ? x.Status.Name : "",
                    FoundByName = x.FoundBy != null ? x.FoundBy.Name : "",
                    x.ClaimedBy
                })
                .ToListAsync();

            vm.Results = rawResults.Select(x => new SearchResultItem
            {
                TrackingId = x.TrackingId,
                DateFound = x.DateFound,
                ItemName = x.ItemName,
                Description = x.Description,
                LocationFound = x.LocationFound,
                RouteName = x.RouteName,
                VehicleName = x.VehicleName,
                StorageLocationName = x.StorageLocationName,
                StatusName = x.StatusName,
                DaysSinceFound = (DateTime.Today - x.DateFound.Date).Days,
                FoundByName = x.FoundByName,
                ClaimedBy = x.ClaimedBy
            }).ToList();

            return View(vm);
        }

        // GET: /LostFoundItem/PrintSearch — full results for print (no pagination)
        [HttpGet]
        public async Task<IActionResult> PrintSearch(SearchViewModel? vm)
        {
            vm ??= new SearchViewModel();

            var query = _context.LostFoundItems.AsQueryable();

            // Use in-memory dropdown lists to populate filter strings to avoid N+1 DB roundtrips.
            await PopulateSearchDropdowns(vm);

            var filters = new List<string>();

            if (vm.TrackingId.HasValue)
            {
                query = query.Where(x => x.TrackingId == vm.TrackingId.Value);
                filters.Add($"Tracking ID: {vm.TrackingId.Value}");
            }
            if (vm.DateFoundFrom.HasValue)
            {
                query = query.Where(x => x.DateFound >= vm.DateFoundFrom.Value);
                filters.Add($"Date Found From: {vm.DateFoundFrom.Value:MM/dd/yyyy}");
            }
            if (vm.DateFoundTo.HasValue)
            {
                query = query.Where(x => x.DateFound <= vm.DateFoundTo.Value);
                filters.Add($"Date Found To: {vm.DateFoundTo.Value:MM/dd/yyyy}");
            }
            if (vm.ItemId.HasValue)
            {
                query = query.Where(x => x.ItemId == vm.ItemId.Value);
                var itemName = vm.Items?.FirstOrDefault(i => i.Value == vm.ItemId.Value.ToString())?.Text ?? vm.ItemId.Value.ToString();
                filters.Add($"Item: {itemName}");
            }
            if (vm.StatusId.HasValue)
            {
                query = query.Where(x => x.StatusId == vm.StatusId.Value);
                var statusName = vm.Statuses?.FirstOrDefault(i => i.Value == vm.StatusId.Value.ToString())?.Text ?? vm.StatusId.Value.ToString();
                filters.Add($"Status: {statusName}");
            }
            if (vm.RouteId.HasValue)
            {
                query = query.Where(x => x.RouteId == vm.RouteId.Value);
                var routeName = vm.Routes?.FirstOrDefault(i => i.Value == vm.RouteId.Value.ToString())?.Text ?? vm.RouteId.Value.ToString();
                filters.Add($"Route #: {routeName}");
            }
            if (vm.VehicleId.HasValue)
            {
                query = query.Where(x => x.VehicleId == vm.VehicleId.Value);
                var vehicleName = vm.Vehicles?.FirstOrDefault(i => i.Value == vm.VehicleId.Value.ToString())?.Text ?? vm.VehicleId.Value.ToString();
                filters.Add($"Vehicle #: {vehicleName}");
            }
            if (vm.StorageLocationId.HasValue)
            {
                query = query.Where(x => x.StorageLocationId == vm.StorageLocationId.Value);
                var locName = vm.StorageLocations?.FirstOrDefault(i => i.Value == vm.StorageLocationId.Value.ToString())?.Text ?? vm.StorageLocationId.Value.ToString();
                filters.Add($"Storage Location: {locName}");
            }
            if (vm.FoundById.HasValue)
            {
                query = query.Where(x => x.FoundById == vm.FoundById.Value);
                var foundName = vm.FoundByNames?.FirstOrDefault(i => i.Value == vm.FoundById.Value.ToString())?.Text ?? vm.FoundById.Value.ToString();
                filters.Add($"Found By: {foundName}");
            }

            vm.FilterSummary = filters.Any() ? string.Join(" | ", filters) : "All Records (No Filters Applied)";

            // Calculate dates using a standard mapped value (EF Core will compute 'Days' safely after pull or map it)
            // SQL Server / SQLite can struggle mapping DateTime.Today natively over EF depending on translation, 
            // but we fetch what we need and materialize cleanly.
            var rawResults = await query
                .OrderByDescending(x => x.TrackingId)
                .Select(x => new
                {
                    x.TrackingId,
                    x.DateFound,
                    ItemName = x.Item != null ? x.Item.Name : "",
                    x.Description,
                    x.LocationFound,
                    RouteName = x.Route != null ? x.Route.Name : "",
                    VehicleName = x.Vehicle != null ? x.Vehicle.Name : "",
                    StorageLocationName = x.StorageLocation != null ? x.StorageLocation.Name : "",
                    StatusName = x.Status != null ? x.Status.Name : "",
                    FoundByName = x.FoundBy != null ? x.FoundBy.Name : "",
                    x.ClaimedBy
                })
                .ToListAsync();

            var results = rawResults.Select(x => new SearchResultItem
            {
                TrackingId = x.TrackingId,
                DateFound = x.DateFound,
                ItemName = x.ItemName,
                Description = x.Description,
                LocationFound = x.LocationFound,
                RouteName = x.RouteName,
                VehicleName = x.VehicleName,
                StorageLocationName = x.StorageLocationName,
                StatusName = x.StatusName,
                DaysSinceFound = (DateTime.Today - x.DateFound.Date).Days,
                FoundByName = x.FoundByName,
                ClaimedBy = x.ClaimedBy
            }).ToList();

            vm.Results = results;
            vm.TotalRecords = results.Count;
            return View(vm);
        }

        // GET: /LostFoundItem/Photo/{fileName}  — authenticated file streaming
        [HttpGet]
        public IActionResult Photo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var result = _fileService.GetPhoto(id);
            if (result == null || result.Value.Stream == null)
                return NotFound();

            return File(result.Value.Stream, result.Value.ContentType);
        }

        // GET: /LostFoundItem/Attachment/{fileName} — authenticated file streaming
        [HttpGet]
        public IActionResult Attachment(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var result = _fileService.GetAttachment(id);
            if (result == null || result.Value.Stream == null)
                return NotFound();

            var fileName = Path.GetFileName(id);
            return File(result.Value.Stream, result.Value.ContentType, fileName);
        }

        // =====================================================================
        // HELPER METHODS
        // =====================================================================

        private async Task PopulateDropdowns(LostFoundItemCreateViewModel vm)
        {
            vm.Items = new SelectList(await _context.Items.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.ItemId);
            vm.Routes = new SelectList(await _context.Routes.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.RouteId);
            vm.Vehicles = new SelectList(await _context.Vehicles.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.VehicleId);
            vm.StorageLocations = new SelectList(await _context.StorageLocations.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.StorageLocationId);
            vm.Statuses = new SelectList(await _context.Statuses.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.StatusId);
            vm.FoundByNames = new SelectList(await _context.FoundByNames.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", vm.FoundById);
        }

        private async Task PopulateSearchDropdowns(SearchViewModel vm)
        {
            vm.Items = new SelectList(await _context.Items.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
            vm.Statuses = new SelectList(await _context.Statuses.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
            vm.Routes = new SelectList(await _context.Routes.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
            vm.Vehicles = new SelectList(await _context.Vehicles.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
            vm.StorageLocations = new SelectList(await _context.StorageLocations.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
            vm.FoundByNames = new SelectList(await _context.FoundByNames.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        }
    }
}

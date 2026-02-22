using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFoundApp.ViewModels
{
    public class LostFoundItemCreateViewModel
    {
        [Required(ErrorMessage = "Date Found is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date Found")]
        public DateTime DateFound { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Item type is required.")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Location Found is required.")]
        [StringLength(300)]
        [Display(Name = "Location Found")]
        public string LocationFound { get; set; } = string.Empty;

        [Display(Name = "Route #")]
        public int? RouteId { get; set; }

        [Display(Name = "Vehicle #")]
        public int? VehicleId { get; set; }

        [Display(Name = "Storage Location")]
        public int? StorageLocationId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public int StatusId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Status Date")]
        public DateTime? StatusDate { get; set; }

        [Display(Name = "Found By")]
        public int? FoundById { get; set; }

        [StringLength(200)]
        [Display(Name = "Claimed By")]
        public string? ClaimedBy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Photo")]
        public IFormFile? PhotoFile { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? AttachmentFile { get; set; }

        // Select lists for dropdowns
        public SelectList? Items { get; set; }
        public SelectList? Routes { get; set; }
        public SelectList? Vehicles { get; set; }
        public SelectList? StorageLocations { get; set; }
        public SelectList? Statuses { get; set; }
        public SelectList? FoundByNames { get; set; }
    }

    public class LostFoundItemEditViewModel : LostFoundItemCreateViewModel
    {
        public int TrackingId { get; set; }

        [Display(Name = "Existing Photo")]
        public string? ExistingPhotoPath { get; set; }

        [Display(Name = "Existing Attachment")]
        public string? ExistingAttachmentPath { get; set; }

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date/Time")]
        public DateTime CreatedDateTime { get; set; }
    }

    public class LostFoundItemDetailViewModel
    {
        public int TrackingId { get; set; }
        public DateTime DateFound { get; set; }
        public string? ItemName { get; set; }
        public string? Description { get; set; }
        public string LocationFound { get; set; } = string.Empty;
        public string? RouteName { get; set; }
        public string? VehicleName { get; set; }
        public string? PhotoPath { get; set; }
        public string? StorageLocationName { get; set; }
        public string? StatusName { get; set; }
        public DateTime? StatusDate { get; set; }
        public int DaysSinceFound { get; set; }
        public string? FoundByName { get; set; }
        public string? ClaimedBy { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string? Notes { get; set; }
        public string? AttachmentPath { get; set; }
    }

    public class SearchViewModel
    {
        // Filter inputs
        [Display(Name = "Tracking ID")]
        public int? TrackingId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Found (From)")]
        public DateTime? DateFoundFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Found (To)")]
        public DateTime? DateFoundTo { get; set; }

        [Display(Name = "Item")]
        public int? ItemId { get; set; }

        [Display(Name = "Status")]
        public int? StatusId { get; set; }

        [Display(Name = "Route #")]
        public int? RouteId { get; set; }

        [Display(Name = "Vehicle #")]
        public int? VehicleId { get; set; }

        [Display(Name = "Storage Location")]
        public int? StorageLocationId { get; set; }

        [Display(Name = "Found By")]
        public int? FoundById { get; set; }

        // Dropdown lists
        public SelectList? Items { get; set; }
        public SelectList? Statuses { get; set; }
        public SelectList? Routes { get; set; }
        public SelectList? Vehicles { get; set; }
        public SelectList? StorageLocations { get; set; }
        public SelectList? FoundByNames { get; set; }

        // Results
        public List<SearchResultItem> Results { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 25;

        // Sort
        public string SortField { get; set; } = "TrackingId";
        public string SortOrder { get; set; } = "desc";

        // Filter summary for print
        public string FilterSummary { get; set; } = string.Empty;
    }

    public class SearchResultItem
    {
        public int TrackingId { get; set; }
        public DateTime DateFound { get; set; }
        public string? ItemName { get; set; }
        public string? Description { get; set; }
        public string LocationFound { get; set; } = string.Empty;
        public string? RouteName { get; set; }
        public string? VehicleName { get; set; }
        public string? StorageLocationName { get; set; }
        public string? StatusName { get; set; }
        public int DaysSinceFound { get; set; }
        public string? FoundByName { get; set; }
        public string? ClaimedBy { get; set; }
    }
}

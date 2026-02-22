namespace LostAndFoundApp.ViewModels
{
    /// <summary>
    /// Role-aware dashboard view model. Contains data for all three role dashboards.
    /// The controller populates only the relevant sections based on the logged-in user's role.
    /// </summary>
    public class DashboardViewModel
    {
        // --- Logged-in user context ---
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // --- Common stats (all roles) ---
        public int TotalItems { get; set; }
        public int FoundCount { get; set; }
        public int ClaimedCount { get; set; }
        public int StoredCount { get; set; }
        public int DisposedCount { get; set; }
        public int TransferredCount { get; set; }

        // --- Recent records (all roles) ---
        public List<DashboardRecentItem> RecentRecords { get; set; } = new();

        // --- SuperAdmin-only: system overview ---
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int LocalUsers { get; set; }
        public int AdUsers { get; set; }
        public int SuperAdminCount { get; set; }
        public int SupervisorCount { get; set; }
        public int UserRoleCount { get; set; }
        public int AdGroupCount { get; set; }
        public int ItemsThisWeek { get; set; }
        public int ItemsThisMonth { get; set; }
        public int UnclaimedOver30Days { get; set; }

        // --- Supervisor-only: master data health ---
        public int MasterItemCount { get; set; }
        public int MasterRouteCount { get; set; }
        public int MasterVehicleCount { get; set; }
        public int MasterStorageLocationCount { get; set; }
        public int MasterStatusCount { get; set; }
        public int MasterFoundByNameCount { get; set; }
        public int ItemsAwaitingAction { get; set; }

        // --- Status breakdown for charts (Supervisor + SuperAdmin) ---
        public List<StatusBreakdownItem> StatusBreakdown { get; set; } = new();

        // --- Top items by type (Supervisor + SuperAdmin) ---
        public List<TopItemType> TopItemTypes { get; set; } = new();
    }

    public class DashboardRecentItem
    {
        public int TrackingId { get; set; }
        public DateTime DateFound { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string LocationFound { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int DaysSinceFound { get; set; }
        public string? ClaimedBy { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class StatusBreakdownItem
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string CssClass { get; set; } = string.Empty;
        public int Percentage { get; set; }
    }

    public class TopItemType
    {
        public string ItemName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

namespace LostAndFoundApp.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalItems { get; set; }
        public int FoundCount { get; set; }
        public int ClaimedCount { get; set; }
        public int StoredCount { get; set; }
        public List<DashboardRecentItem> RecentRecords { get; set; } = new();
    }

    public class DashboardRecentItem
    {
        public int TrackingId { get; set; }
        public DateTime DateFound { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string LocationFound { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int DaysSinceFound { get; set; }
    }
}

namespace RetailMonolith.Services
{
    public interface IDummyDataService
    {
        Task<DummyDataResult> GenerateDummyOrdersAsync(int count = 50);
        Task<int> CleanupDummyDataAsync();
    }

    public class DummyDataResult
    {
        public int OrdersCreated { get; set; }
        public int OrderLinesCreated { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime EarliestOrderDate { get; set; }
        public DateTime LatestOrderDate { get; set; }
    }
}

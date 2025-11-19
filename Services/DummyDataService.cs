using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Models;

namespace RetailMonolith.Services
{
    public class DummyDataService : IDummyDataService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DummyDataService> _logger;
        private readonly Random _random = new Random();

        public DummyDataService(AppDbContext db, ILogger<DummyDataService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DummyDataResult> GenerateDummyOrdersAsync(int count = 50)
        {
            _logger.LogInformation("Generating {Count} dummy orders...", count);

            // Get all active products
            var activeProducts = await _db.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            if (!activeProducts.Any())
            {
                throw new InvalidOperationException("No active products available to generate dummy orders.");
            }

            var orders = new List<Order>();
            var now = DateTime.UtcNow;
            var earliestDate = now.AddDays(-30);
            int totalOrderLines = 0;
            decimal totalRevenue = 0;

            var statuses = new[] { "Created", "Paid", "Paid", "Paid", "Shipped", "Shipped" }; // Weight towards Paid/Shipped

            for (int i = 0; i < count; i++)
            {
                // Generate random customer ID
                var customerId = $"testuser_{_random.Next(1000, 9999)}";

                // Random date within last 30 days
                var orderDate = earliestDate.AddSeconds(_random.NextDouble() * (now - earliestDate).TotalSeconds);

                // Random number of products (1-5)
                var productCount = _random.Next(1, 6);
                var selectedProducts = activeProducts
                    .OrderBy(x => _random.Next())
                    .Take(productCount)
                    .ToList();

                var orderLines = new List<OrderLine>();
                decimal orderTotal = 0;

                foreach (var product in selectedProducts)
                {
                    var quantity = _random.Next(1, 4); // 1-3 items per product
                    var lineTotal = product.Price * quantity;
                    orderTotal += lineTotal;

                    orderLines.Add(new OrderLine
                    {
                        Sku = product.Sku,
                        Name = product.Name,
                        UnitPrice = product.Price,
                        Quantity = quantity
                    });
                }

                totalOrderLines += orderLines.Count;
                totalRevenue += orderTotal;

                orders.Add(new Order
                {
                    CustomerId = customerId,
                    CreatedUtc = orderDate,
                    Status = statuses[_random.Next(statuses.Length)],
                    Total = orderTotal,
                    Lines = orderLines
                });
            }

            // Save to database
            await _db.Orders.AddRangeAsync(orders);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully generated {Count} dummy orders with {LineCount} order lines", 
                count, totalOrderLines);

            return new DummyDataResult
            {
                OrdersCreated = count,
                OrderLinesCreated = totalOrderLines,
                TotalRevenue = totalRevenue,
                EarliestOrderDate = earliestDate,
                LatestOrderDate = now
            };
        }

        public async Task<int> CleanupDummyDataAsync()
        {
            _logger.LogInformation("Cleaning up dummy data (testuser_ prefix)...");

            // Find all orders with testuser_ prefix
            var dummyOrders = await _db.Orders
                .Where(o => o.CustomerId.StartsWith("testuser_"))
                .ToListAsync();

            var count = dummyOrders.Count;

            if (count > 0)
            {
                _db.Orders.RemoveRange(dummyOrders);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} dummy orders", count);
            }
            else
            {
                _logger.LogInformation("No dummy orders found to clean up");
            }

            return count;
        }
    }
}

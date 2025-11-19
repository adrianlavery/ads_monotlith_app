using RetailMonolith.Models;

namespace RetailMonolith.Services
{
    public interface ICartService
    {
        Task<Cart> GetOrCreateCartAsync(string customerId, CancellationToken ct = default);
        Task AddToCartAsync(string customerId, int productId, int quantity = 1, CancellationToken ct = default);
        Task<Cart> GetCartWithLinesAsync(string customerId, CancellationToken ct = default);
        Task ClearCartAsync(string customerId, CancellationToken ct = default);
        Task UpdateQuantityAsync(string customerId, int cartLineId, int newQuantity, CancellationToken ct = default);
        Task RemoveItemAsync(string customerId, int cartLineId, CancellationToken ct = default);
    }
}

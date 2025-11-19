using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Models;
using RetailMonolith.Services;
using System.Threading.Tasks;

namespace RetailMonolith.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly ICartService _cartService;
        private readonly ISearchService _searchService;

        public IndexModel(AppDbContext db, ICartService cartService, ISearchService searchService)
        {
            _db = db;
            _cartService = cartService;
            _searchService = searchService;
        }

        public IList<Product> Products { get; set; } = new List<Product>();

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowTracing { get; set; }

        public SearchResponse? SearchResponse { get; set; }

        // Category ? Image mapping
        public static readonly Dictionary<string, string> CategoryImages = new()
        {
            ["Beauty"] = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?auto=format&fit=crop&w=800&q=80",
            ["Apparel"] = "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?auto=format&fit=crop&w=800&q=80",
            ["Footwear"] = "https://images.unsplash.com/photo-1603808033192-082d6919d3e1?auto=format&fit=crop&w=800&q=80",
            ["Home"] = "https://images.unsplash.com/photo-1583847268964-b28dc8f51f92?auto=format&fit=crop&w=800&q=80",
            ["Accessories"] = "https://images.unsplash.com/photo-1586878341523-7acb55eb8c12?auto=format&fit=crop&w=800&q=80",
            ["Electronics"] = "https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&w=800&q=80"
        };

        // Helper method accessible from the Razor page
        public string GetImageForCategory(string category)
        {
            if (CategoryImages.TryGetValue(category ?? string.Empty, out var url))
                return url;

            // Fallback image if category missing
            return "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?auto=format&fit=crop&w=800&q=80";
        }

        public async Task OnGetAsync() 
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                if (ShowTracing)
                {
                    // Use semantic search with tracing
                    SearchResponse = await _searchService.SearchProductsWithTraceAsync(SearchQuery);
                    Products = SearchResponse.Results.Select(r => r.Product).ToList();
                    
                    // Apply category filter if specified
                    if (!string.IsNullOrWhiteSpace(Category))
                    {
                        Products = Products.Where(p => p.Category == Category).ToList();
                    }
                }
                else
                {
                    // Use semantic search without tracing
                    var searchResults = await _searchService.SearchProductsAsync(SearchQuery);
                    Products = searchResults.ToList();
                    
                    // Apply category filter if specified
                    if (!string.IsNullOrWhiteSpace(Category))
                    {
                        Products = Products.Where(p => p.Category == Category).ToList();
                    }
                }
            }
            else
            {
                // Show products filtered by category or all active products
                var query = _db.Products.Where(p => p.IsActive);
                
                if (!string.IsNullOrWhiteSpace(Category))
                {
                    query = query.Where(p => p.Category == Category);
                }
                
                Products = await query.ToListAsync();
            }
        }

        public async Task OnPostAsync(int productId)
        {
            // Add to cart logic will go here in the future
            var p = await _db.Products.FindAsync(productId);
            if (p is null) return;

            var cart = await _db.Carts
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.CustomerId == "guest")
                ?? new Models.Cart { CustomerId = "guest" };

            //if(cart.Id == 0)
            //{
            //    _db.Carts.Add(cart);
            //};

            if (cart is null)
            {
                cart = new Models.Cart { CustomerId = "guest" };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            cart.Lines.Add(new CartLine
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.Price,
                Quantity = 1
            });
            await _cartService.AddToCartAsync("guest", productId);
            Response.Redirect("/Cart");
        }
    }
}

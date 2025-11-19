using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Services;

namespace RetailMonolith.Pages.Cart
{
    public class IndexModel : PageModel
    {
       
      
        private readonly ICartService _cartService;
       
        public IndexModel(ICartService cartService)
        {
            _cartService = cartService;
        }



        //map the cart state in memory without mapping directly to the database
        // Each line represents an item in the cart with its ID, name, quantity, and price
        public List<(int Id, string Name, int Quantity, decimal Price)> Lines { get; set; } = new(); 

        public decimal Total => Lines.Sum(line => line.Price * line.Quantity);


        public async Task OnGetAsync()
        {
            var cart = await _cartService.GetCartWithLinesAsync("guest");
            Lines = cart.Lines
                .Select(line => (line.Id, line.Name, line.Quantity, line.UnitPrice))
                .ToList();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartLineId, int newQuantity)
        {
            if (newQuantity < 0) newQuantity = 0;
            await _cartService.UpdateQuantityAsync("guest", cartLineId, newQuantity);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveItemAsync(int cartLineId)
        {
            await _cartService.RemoveItemAsync("guest", cartLineId);
            return RedirectToPage();
        }

    }
}

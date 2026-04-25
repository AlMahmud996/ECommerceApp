using Microsoft.AspNetCore.Mvc;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Checkout()
        {
            var sessionId = HttpContext.Session.Id;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            if (cartItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            ViewBag.CartItems = cartItems;
            ViewBag.Total = cartItems.Sum(i => i.Product.Price * i.Quantity);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string customerName, string customerEmail, string address)
        {
            var sessionId = HttpContext.Session.Id;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            if (cartItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            var order = new Order
            {
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                Address = address,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(i => i.Product.Price * i.Quantity),
                OrderItems = cartItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("Summary", new { id = order.Id });
        }

        public async Task<IActionResult> Summary(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index", "Home");

            return View(order);
        }
    }
}
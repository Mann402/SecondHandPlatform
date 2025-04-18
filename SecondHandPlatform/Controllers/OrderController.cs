using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public OrderController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // GET /api/Order/User/{userId}
        // Returns all orders placed by this user (the BUYER), with line-items and seller info
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<OrderDto>>> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User) // BUYER nav
                .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Product)
                     .ThenInclude(p => p.User) // SELLER nav
                .Include(o => o.Payments)
                .ToListAsync();

            if (!orders.Any())
                return NotFound("No orders found for this user.");

            var result = orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus ?? "",
                BuyerId = o.UserId,
                BuyerName = $"{o.User?.FirstName ?? ""} {o.User?.LastName ?? ""}".Trim(),

                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.ProductName ?? "",
                    SellerName = $"{oi.Product?.User?.FirstName ?? ""} {oi.Product?.User?.LastName ?? ""}"
                          .Trim(),
                    Price = oi.Product?.ProductPrice ?? 0m,
                    Quantity = oi.Quantity,
                    ProductImage = oi.Product?.ProductImage != null
       ? Convert.ToBase64String(oi.Product.ProductImage)
       : null

                }).ToList(),

                PaymentMethod = o.Payments.FirstOrDefault()?.PaymentMethod ?? "Not specified",
                PaymentDate = o.Payments.FirstOrDefault()?.PaymentDate
            })
            .ToList();

            return Ok(result);
        }

        // GET /api/Order/Seller/{userId}
        // Returns all orders for products this user (the SELLER) has listed
        [HttpGet("seller/{userId}")]
        public async Task<ActionResult<List<OrderDto>>> GetSellerOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.User) // BUYER nav
                .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Product)
                     .ThenInclude(p => p.User) // SELLER nav
                .Include(o => o.Payments)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                .ToListAsync();

            if (!orders.Any())
                return NotFound("No sales orders found for this seller.");

            var result = orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus ?? "",
                BuyerId = o.UserId,
                BuyerName = $"{o.User?.FirstName ?? ""} {o.User?.LastName ?? ""}".Trim(),

                Items = o.OrderItems
                    .Where(oi => oi.Product?.UserId == userId) // only this seller's items
                    .Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product?.ProductName ?? "",
                        SellerName = $"{oi.Product?.User?.FirstName ?? ""} {oi.Product?.User?.LastName ?? ""}"
                          .Trim(),
                        Price = oi.Product?.ProductPrice ?? 0m,
                        Quantity = oi.Quantity
                    })
                    .ToList(),

                PaymentMethod = o.Payments.FirstOrDefault()?.PaymentMethod ?? "Not specified",
                PaymentDate = o.Payments.FirstOrDefault()?.PaymentDate
            })
            .ToList();

            return Ok(result);
        }

        // PUT /api/Order/{id}/receive
        // This endpoint marks an order as "Completed" when a user receives the product
        [HttpPut("{id}/receive")]
        public async Task<IActionResult> ReceiveProduct(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            if (order.OrderStatus != "Processing")
            {
                return BadRequest("Only processing orders can be marked as received.");
            }

            // Update the order status to Completed
            order.OrderStatus = "Completed";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated to Completed." });
        }

        // POST /api/Order
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest orderRequest)
        {
            if (orderRequest == null || orderRequest.UserId == 0 || orderRequest.CartId == 0)
            {
                return BadRequest("UserId and CartId are required.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Console.WriteLine("🚀 Debug: Transaction Started");

                var cartItems = await _context.Carts
                    .Where(c => c.UserId == orderRequest.UserId)
                    .Include(c => c.Product)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return BadRequest("Your cart is empty. Add products before placing an order.");
                }

                decimal totalAmount = 0;
                List<Order> newOrders = new List<Order>();

                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product;
                    if (product == null)
                    {
                        return BadRequest($"Product ID {cartItem.ProductId} not found.");
                    }

                    // Check if product is already ordered
                    bool isProductOrdered = await _context.Orders.AnyAsync(o => o.ProductId == product.ProductId);
                    if (isProductOrdered)
                    {
                        return BadRequest($"The product '{product.ProductName}' has already been ordered by another user.");
                    }

                    if (product.IsSold)
                    {
                        return BadRequest($"The product '{product.ProductName}' is already sold.");
                    }
                    if (product.ProductStatus == "Rejected")
                    {
                        return BadRequest($"The product '{product.ProductName}' is rejected.");
                    }

                    totalAmount += product.ProductPrice;

                    // Create new order with "Processing" status directly
                    var newOrder = new Order
                    {
                        UserId = orderRequest.UserId,
                        ProductId = product.ProductId,
                        CartId = cartItem.CartId,
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = product.ProductPrice,
                        OrderStatus = "Processing"
                        // Note: Payment details are now handled in the Payment table
                    };

                    newOrders.Add(newOrder);
                }

                Console.WriteLine($"🚀 Debug: Orders to Insert: {newOrders.Count}");

                _context.Orders.AddRange(newOrders);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Debug: Orders Saved to Database");

                // Mark Product as Sold
                foreach (var cartItem in cartItems)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        product.IsSold = true;
                        product.ProductStatus = "Sold"; // Update product status directly
                        _context.Products.Update(product);
                    }
                }
                await _context.SaveChangesAsync();

                // Create payment records if payment method was provided
                if (!string.IsNullOrEmpty(orderRequest.PaymentMethod))
                {
                    foreach (var order in newOrders)
                    {
                        var payment = new Payment
                        {
                            OrderId = order.OrderId,
                            PaymentMethod = orderRequest.PaymentMethod,
                            PaymentStatus = "Completed",
                            Amount = order.TotalAmount,
                            PaymentDate = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                    }
                    await _context.SaveChangesAsync();
                }

                // Remove purchased cart items
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Debug: Cart Items Removed");

                await transaction.CommitAsync();
                Console.WriteLine("🎉 Debug: Transaction Committed");

                return Ok(new
                {
                    message = "Order placed successfully!",
                    totalAmount,
                    orderId = newOrders.FirstOrDefault()?.OrderId // Return the first order ID for redirection
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error: {ex.Message}");
                return StatusCode(500, $"Error saving order: {ex.Message}");
            }
        }

        // DELETE /api/Order/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            if (order.OrderStatus == "Completed")
            {
                return BadRequest("Cannot cancel a completed order.");
            }

            // Update product status when cancelling order
            var product = await _context.Products.FindAsync(order.ProductId);
            if (product != null)
            {
                product.IsSold = false;
                product.ProductStatus = "Available"; // Reset product status to available
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Order canceled successfully!" });
        }
    }

    // DTO for order requests
    public class OrderRequest
    {
        public int UserId { get; set; }
        public int CartId { get; set; }
        public string PaymentMethod { get; set; }
        public string StripePaymentIntentId { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = "";

        // buyer info
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = "";

        // line items
        public List<OrderItemDto> Items { get; set; } = new();

        // payment
        public string PaymentMethod { get; set; } = "";
        public DateTime? PaymentDate { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ProductImage { get; set; }

    }

}
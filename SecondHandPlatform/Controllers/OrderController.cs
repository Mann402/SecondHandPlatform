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
            // First, just get all the order IDs
            var orderIds = new List<int>();

            // Get order IDs in a separate connection
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"SELECT order_id FROM orders WHERE user_id = {userId}";

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orderIds.Add(reader.GetInt32(0));
                    }
                }
            }

            // Now process each order separately
            var orders = new List<OrderDto>();

            foreach (var orderId in orderIds)
            {
                var orderDto = new OrderDto
                {
                    OrderId = orderId,
                    Items = new List<OrderItemDto>()
                };

                // Get order details
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                SELECT o.order_date, o.order_status, o.user_id,  , o.total_amount 
                       u.first_name, u.last_name, 
                       p.payment_method, p.payment_date
                FROM orders o
                LEFT JOIN users u ON o.user_id = u.user_id
                LEFT JOIN payments p ON o.order_id = p.order_id
                WHERE o.order_id = " + orderId;

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            orderDto.OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date"));
                            orderDto.OrderStatus = reader.IsDBNull(reader.GetOrdinal("order_status")) ?
                                "" : reader.GetString(reader.GetOrdinal("order_status"));
                            orderDto.BuyerId = reader.GetInt32(reader.GetOrdinal("user_id"));
                            orderDto.BuyerName = string.Format("{0} {1}",
                                reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString(reader.GetOrdinal("first_name")),
                                reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString(reader.GetOrdinal("last_name"))
                            ).Trim();
                            orderDto.PaymentMethod = reader.IsDBNull(reader.GetOrdinal("payment_method")) ?
                                "Not specified" : reader.GetString(reader.GetOrdinal("payment_method"));
                            orderDto.PaymentDate = reader.IsDBNull(reader.GetOrdinal("payment_date")) ?
                                null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("payment_date"));
                        }
                    }
                }

                // Get order items
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                SELECT oi.product_id, oi.quantity, 
                       p.product_name, p.product_price, p.product_image,
                       u.first_name as seller_first_name, u.last_name as seller_last_name
                FROM orderitems oi
                JOIN products p ON oi.product_id = p.product_id
                LEFT JOIN users u ON p.user_id = u.user_id
                WHERE oi.order_id = " + orderId;

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            byte[] productImage = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("product_image")))
                            {
                                productImage = (byte[])reader["product_image"];
                            }

                            orderDto.Items.Add(new OrderItemDto
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                                ProductName = reader.IsDBNull(reader.GetOrdinal("product_name")) ?
                                    "" : reader.GetString(reader.GetOrdinal("product_name")),
                                SellerName = string.Format("{0} {1}",
                                    reader.IsDBNull(reader.GetOrdinal("seller_first_name")) ? "" : reader.GetString(reader.GetOrdinal("seller_first_name")),
                                    reader.IsDBNull(reader.GetOrdinal("seller_last_name")) ? "" : reader.GetString(reader.GetOrdinal("seller_last_name"))
                                ).Trim(),
                                Price = reader.IsDBNull(reader.GetOrdinal("product_price")) ?
                                    0m : reader.GetDecimal(reader.GetOrdinal("product_price")),
                                Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                ProductImage = productImage != null ? Convert.ToBase64String(productImage) : null
                            });
                        }
                    }
                }

                orders.Add(orderDto);
            }

            return Ok(orders);
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
                .Include(o => o.Payment)
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

                PaymentMethod = o.Payment?.PaymentMethod ?? "Not specified",
                PaymentDate = o.Payment?.PaymentDate
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
            if (orderRequest == null || orderRequest.UserId == 0 || orderRequest.CartIds == null || !orderRequest.CartIds.Any())
            {
                return BadRequest("UserId and CartId are required.");
            }

            // 1) load and validate cart items
            var cartItems = await _context.Carts
                .Where(c => orderRequest.CartIds.Contains(c.CartId))
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("Your cart is empty. Add products before placing an order.");

            foreach (var c in cartItems)
            {
                var p = c.Product
                     ?? throw new InvalidOperationException($"Product {c.ProductId} missing.");

                // check via OrderItems now:
                bool alreadyOrdered = await _context.OrderItems
                    .AnyAsync(oi => oi.ProductId == p.ProductId);
                if (alreadyOrdered)
                    return BadRequest($"'{p.ProductName}' was already ordered.");

                if (p.IsSold)
                    return BadRequest($"'{p.ProductName}' is already sold.");

                if (p.ProductStatus == "Rejected")
                    return BadRequest($"'{p.ProductName}' is rejected.");
            }

            // 2) sum total
            decimal totalAmount = cartItems.Sum(c => c.Product!.ProductPrice);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3) create one Order
                var order = new Order
                {
                    UserId = orderRequest.UserId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    OrderStatus = "Processing"
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();  // order.OrderId now set

                // 4) create N OrderItems
                var orderItems = cartItems.Select(c => new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = c.ProductId,
                    Quantity = 1
                });
                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                // 5) record a single Payment
                if (!string.IsNullOrEmpty(orderRequest.PaymentMethod))
                {
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = orderRequest.PaymentMethod,
                        PaymentStatus = "Completed",
                        Amount = totalAmount,
                        PaymentDate = DateTime.UtcNow
                    };
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }

                // 6) mark products sold
                foreach (var c in cartItems)
                {
                    var prod = await _context.Products.FindAsync(c.ProductId);
                    if (prod != null)
                    {
                        prod.IsSold = true;
                        prod.ProductStatus = "Sold";
                    }
                }
                await _context.SaveChangesAsync();

                // 7) remove cart entries
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return Ok(new
                {
                    message = "Order placed successfully!",
                    orderId = order.OrderId,
                    totalAmount
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error placing order: {ex.Message}");
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

            // load line items
            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            // un-sell each product
            foreach (var oi in items)
            {
                var p = await _context.Products.FindAsync(oi.ProductId);
                if (p != null)
                {
                    p.IsSold = false;
                    p.ProductStatus = "Available";
                }
            }
            await _context.SaveChangesAsync();

            // delete orderitems + order
            _context.OrderItems.RemoveRange(items);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order canceled successfully!" });
        }
    }

    // DTO for order requests
    public class OrderRequest
    {
        public int UserId { get; set; }
        public List<int> CartIds { get; set; }
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
        public decimal TotalAmount { get; set; }

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
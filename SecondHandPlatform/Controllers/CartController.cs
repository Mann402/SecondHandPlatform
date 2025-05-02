// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public CartController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // DTO for the Product portion of the cart item
        public class ProductDto
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = null!;
            public decimal ProductPrice { get; set; }
            public string Status { get; set; } = null!;  // maps from Product.ProductStatus
            public string? ProductImage { get; set; }           // Base64 string, or null
        }

        // DTO for each cart entry
        public class CartItemDto
        {
            public ProductDto Product { get; set; } = null!;
        }

        // GET: api/Cart/User/5
        [HttpGet("User/{userId}")]
        public async Task<ActionResult<List<CartItemDto>>> GetUserCart(int userId)
        {
            try
            {
                var raw = await _context.Carts
                    .AsNoTracking()
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                var dto = raw
                  .Select(c => new CartItemDto
                  {
                      Product = new ProductDto
                      {
                          ProductId = c.Product.ProductId,
                          ProductName = c.Product.ProductName,
                          ProductPrice = c.Product.ProductPrice,
                          Status = c.Product.ProductStatus,
                          ProductImage = c.Product.ProductImage is null
                                          ? null
                                          : Convert.ToBase64String(c.Product.ProductImage)
                      }
                  })
                  .ToList();

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CartController] Error fetching cart for user {userId}: {ex}");
                return StatusCode(500, new
                {
                    message = "Error retrieving cart",
                    error = ex.Message
                });
            }
        }

        // POST: api/Cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] Cart cartItem)
        {
            try
            {
                if (cartItem == null || cartItem.UserId == 0 || cartItem.ProductId == 0)
                    return BadRequest("UserId and ProductId are required.");

                var user = await _context.Users.FindAsync(cartItem.UserId);
                var product = await _context.Products.FindAsync(cartItem.ProductId);

                if (user == null) return NotFound("User not found.");
                if (product == null) return NotFound("Product not found.");

                var exists = await _context.Carts
                    .AnyAsync(c => c.UserId == cartItem.UserId && c.ProductId == cartItem.ProductId);
                if (exists)
                    return BadRequest("Product is already in the cart.");

                _context.Carts.Add(new Cart
                {
                    UserId = cartItem.UserId,
                    ProductId = cartItem.ProductId,
                    TotalPrice = product.ProductPrice
                });
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product added to cart!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding item to cart: {ex.Message}");
                return StatusCode(500, new { message = "Error adding product to cart", error = ex.Message });
            }
        }

        // DELETE: api/Cart/5/123
        [HttpDelete("{userId}/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int userId, int productId)
        {
            try
            {
                var cartEntry = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartEntry == null)
                    return NotFound("Cart item not found.");

                _context.Carts.Remove(cartEntry);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product removed from cart!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing item from cart: {ex.Message}");
                return StatusCode(500, new { message = "Error removing product from cart", error = ex.Message });
            }
        }
    }
}

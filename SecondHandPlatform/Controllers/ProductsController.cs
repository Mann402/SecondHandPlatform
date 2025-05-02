using Microsoft.AspNetCore.Mvc;
using SecondHandPlatform.Interfaces;
using SecondHandPlatform.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
            private readonly ILogger<ProductsController> _logger; 

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // GET /api/Products/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetProductById(int id)
        {
            var p = await _productRepository.GetProductByIdAsync(id);
            if (p == null) return NotFound();
            return Ok(new
            {
                p.ProductId,
                p.ProductName,
                Category = p.Category.Name,
                p.ProductDescription,
                p.ProductCondition,
                Price = (p.ProductStatus == "Verified" && p.VerifiedPrice.HasValue)
                                ? p.VerifiedPrice.Value
                                : p.ProductPrice,
                ImageBase64 = p.ProductImage != null
                                ? Convert.ToBase64String(p.ProductImage)
                                : null,

                isVerificationRequested = p.IsVerificationRequested,
                verificationRequestedDate = p.VerificationRequestedDate,
                productStatus = p.ProductStatus,
                datePosted = p.DatePosted,
                isSold = p.IsSold
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            var all = await _productRepository.GetAllProductsAsync();

            // Project into a lightweight DTO with a single Price field
            var shaped = all.Select(p => new {
                p.ProductId,
                p.ProductName,
                Category = p.Category.Name,
                Price = (p.ProductStatus == "Verified" && p.VerifiedPrice.HasValue)
                            ? p.VerifiedPrice.Value
                            : p.ProductPrice,
                ImageBase64 = p.ProductImage != null
                            ? Convert.ToBase64String(p.ProductImage)
                            : null,
                productStatus = p.ProductStatus,
                datePosted = p.DatePosted
            });

            return Ok(shaped);
        }


        [HttpGet("User/{userId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserProducts(int userId)
        {
            var list = await _productRepository.GetUserProductsAsync(userId);
            var shaped = list.Select(p => new {
                p.ProductId,
                p.ProductName,
                category = p.Category.Name,
                price = (p.ProductStatus == "Verified" && p.VerifiedPrice.HasValue)
                                 ? p.VerifiedPrice.Value
                                 : p.ProductPrice,
                productStatus = p.ProductStatus
            });
            return Ok(shaped);
        }
      

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            if (product == null)
                return BadRequest("Invalid product data.");

            await _productRepository.AddProductAsync(product);
            return Ok(new { message = "Product added successfully and is pending verification!" });
        }

      
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            // apply only the fields we expect
            product.ProductName = dto.ProductName;
            product.ProductDescription = dto.ProductDescription;
            product.CategoryId = dto.CategoryId;
            product.ProductPrice = dto.ProductPrice;
            product.ProductCondition = dto.ProductCondition;
            product.ProductImage = dto.ProductImage;
            product.ProductStatus = "Unverified";

            await _productRepository.UpdateProductAsync(product);
            return Ok();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productRepository.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully!" });
        }

        [HttpPut("Verify/{productId}")]
        public async Task<IActionResult> VerifyProduct(int productId)
        {
            await _productRepository.VerifyProductAsync(productId);
            return Ok(new { message = "Product verified successfully!" });
        }

        [HttpPut("Reject/{productId}")]
        public async Task<IActionResult> RejectProduct(int productId)
        {
            await _productRepository.RejectProductAsync(productId);
            return Ok(new { message = "Product rejected successfully!" });
        }
        // ProductsController.cs
        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetImage(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product?.ProductImage == null || product.ProductImage.Length == 0)
                return NotFound();

            // If you know it’s always a JPEG:
            return File(product.ProductImage, "image/jpeg");
        }

        // GET /api/Products/category/{slug}
        [HttpGet("category/{slug}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByCategory(string slug)
        {
            try { 
            var list = await _productRepository.GetByCategorySlugAsync(slug);
            var shaped = list.Select(p => new {
                p.ProductId,
                p.ProductName,
                Category = p.Category.Name,
                Price = (p.ProductStatus == "Verified" && p.VerifiedPrice.HasValue)
                                ? p.VerifiedPrice.Value
                                : p.ProductPrice,
                ImageBase64 = p.ProductImage != null
                                ? Convert.ToBase64String(p.ProductImage)
                                : null,
                productStatus = p.ProductStatus,
                datePosted = p.DatePosted

            });
            return Ok(shaped);
        }
            catch (Exception ex)
            {
                
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        public class RequestVerificationDto
        {
            public int UserId { get; set; }
        }

        [HttpPost("{id}/requestVerification")]
        public async Task<IActionResult> RequestVerification(
            int id,
            [FromBody] RequestVerificationDto dto)
        {
            var p = await _productRepository.GetProductByIdAsync(id);
            if (p == null) return NotFound();

            p.IsVerificationRequested = true;
            p.VerificationRequestedDate = DateTime.UtcNow;
            await _productRepository.UpdateProductAsync(p);

            return Ok(new { message = "Your request has been sent to admin." });
        }


        public class ProductUpdateDto
        {
            public string ProductName { get; set; } = null!;
            public string ProductDescription { get; set; } = null!;
            public int CategoryId { get; set; }         // NEW
            public decimal ProductPrice { get; set; }
            public string ProductCondition { get; set; } = null!;
            public byte[]? ProductImage { get; set; }
        }

    }
}

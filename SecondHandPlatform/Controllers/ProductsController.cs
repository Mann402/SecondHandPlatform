using Microsoft.AspNetCore.Mvc;
using SecondHandPlatform.Interfaces;
using SecondHandPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }



        // GET /api/Products/{id} <--- Add this
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            return Ok(product);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var all = await _productRepository.GetAllProductsAsync();

            // Project into a lightweight DTO with a single Price field
            var shaped = all.Select(p => new {
                p.ProductId,
                p.ProductName,
                Price = (p.ProductStatus == "Verified" && p.VerifiedPrice.HasValue)
                            ? p.VerifiedPrice.Value
                            : p.ProductPrice,
                ImageBase64 = p.ProductImage != null
                            ? Convert.ToBase64String(p.ProductImage)
                            : null,
                p.ProductStatus
            });

            return Ok(shaped);
        }
        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetUserProducts(int userId)
        {
            var products = await _productRepository.GetUserProductsAsync(userId);
            return Ok(products);
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
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            // Update fields
            product.ProductName = updatedProduct.ProductName;
            product.ProductDescription = updatedProduct.ProductDescription;
            product.ProductCategory = updatedProduct.ProductCategory;
            product.ProductPrice = updatedProduct.ProductPrice;
            product.ProductCondition = updatedProduct.ProductCondition;
            product.ProductImage = updatedProduct.ProductImage;
            product.ProductStatus = "Pending Verification";

            await _productRepository.UpdateProductAsync(product);
            return Ok(new { message = "Product updated successfully!" });
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

    }
}

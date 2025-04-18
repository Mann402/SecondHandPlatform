using SecondHandPlatform.Interfaces;
using SecondHandPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatform.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Product>> GetVerifiedProductsAsync()
        {
            return await _productRepository.GetVerifiedProductsAsync();
        }

        public async Task<IEnumerable<Product>> GetUserProductsAsync(int userId)
        {
            return await _productRepository.GetUserProductsAsync(userId);
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetProductByIdAsync(id);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllProductsAsync();
        }

        public async Task AddProductAsync(Product product)
        {
            product.DatePosted = DateTime.UtcNow;
            product.ProductStatus = "Unverified";
            product.IsSold = false;
            await _productRepository.AddProductAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            product.ProductStatus = "Unverified"; // Reset to pending if updated
            await _productRepository.UpdateProductAsync(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            await _productRepository.DeleteProductAsync(id);
        }

        public async Task VerifyProductAsync(int productId)
        {
            await _productRepository.VerifyProductAsync(productId);
        }

        public async Task RejectProductAsync(int productId)
        {
            await _productRepository.RejectProductAsync(productId);
        }
    }
}

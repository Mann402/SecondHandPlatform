using SecondHandPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatform.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetVerifiedProductsAsync();
        Task<IEnumerable<Product>> GetUserProductsAsync(int userId);
        Task<Product> GetProductByIdAsync(int id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task VerifyProductAsync(int productId);
        Task RejectProductAsync(int productId);
        Task<IEnumerable<Product>> GetByCategorySlugAsync(string slug);

    }
}

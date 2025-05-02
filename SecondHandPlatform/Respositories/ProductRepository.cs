using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Interfaces;
using SecondHandPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;


namespace SecondHandPlatform.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly SecondhandplatformContext _context;

        public ProductRepository(SecondhandplatformContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetVerifiedProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductStatus == "Verified" && !p.IsSold)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetUserProductsAsync(int userId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }



        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsSold)          // omit sold items if you still want only available products
                .ToListAsync();
        }

        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task VerifyProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.ProductStatus = "Verified";
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.ProductStatus = "Rejected";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && !p.IsSold)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCategorySlugAsync(string slug)
        {
            // normalize once in C#
            var slugLower = slug.Trim().ToLower();

            // case-insensitive compare on Category.Slug
            return await _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.Category.Slug.ToLower() == slugLower
                    && !p.IsSold
                )
                .ToListAsync();
        }
    }
}


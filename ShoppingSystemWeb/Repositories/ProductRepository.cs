#nullable disable
using Microsoft.EntityFrameworkCore;
using ShoppingSystemWeb.Data;
using ShoppingSystemWeb.Models;
using ShoppingSystemWeb.Services;

namespace ShoppingSystemWeb.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShoppingSystemWebContext _context;

        public ProductRepository(ShoppingSystemWebContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<Product>> GetPagedAsync(string searchString, int pageIndex, int pageSize)
        {
            IQueryable<Product> products = _context.Product;

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Title.Contains(searchString) || s.Category.Contains(searchString));
            }

            products = products.OrderBy(p => p.Id);

            return await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), pageIndex, pageSize);
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Product.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Product.AnyAsync(e => e.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product != null)
            {
                _context.Product.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
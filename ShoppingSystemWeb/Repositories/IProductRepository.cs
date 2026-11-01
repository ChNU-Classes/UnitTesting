#nullable disable
using ShoppingSystemWeb.Models;
using ShoppingSystemWeb.Services;

namespace ShoppingSystemWeb.Repositories
{
    public interface IProductRepository
    {
        Task<PaginatedList<Product>> GetPagedAsync(string searchString, int pageIndex, int pageSize);
        Task<Product> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
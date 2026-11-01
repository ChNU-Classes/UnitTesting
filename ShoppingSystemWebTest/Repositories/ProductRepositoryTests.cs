using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShoppingSystemWeb.Data;
using ShoppingSystemWeb.Models;
using ShoppingSystemWeb.Repositories;
using Xunit;

namespace ShoppingSystemWebTest.Repositories
{
    // CRUD-тести на реальному EF Core + SQLite in-memory.
    // Для кожного тесту створюється нова порожня БД.
    public class ProductRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ShoppingSystemWebContext> _options;

        public ProductRepositoryTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ShoppingSystemWebContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ShoppingSystemWebContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private ShoppingSystemWebContext NewContext() => new ShoppingSystemWebContext(_options);

        private static Product NewProduct(string title, string category, decimal price = 10m) => new Product
        {
            Title = title,
            Category = category,
            Price = price,
            ExpiredDate = new DateTime(2030, 1, 1)
        };

        private void Seed(params Product[] products)
        {
            using var context = NewContext();
            context.Product.AddRange(products);
            context.SaveChanges();
        }

        // ---------- Create ----------

        [Fact]
        public async Task AddAsync_SavesProductAndGeneratesId()
        {
            var product = NewProduct("Fresh milk", "Milk food", 25.5m);

            using (var context = NewContext())
            {
                await new ProductRepository(context).AddAsync(product);
            }

            Assert.True(product.Id > 0);
            using var check = NewContext();
            var saved = await check.Product.SingleAsync();
            Assert.Equal("Fresh milk", saved.Title);
            Assert.Equal("Milk food", saved.Category);
            Assert.Equal(25.5m, saved.Price);
        }

        // ---------- Read ----------

        [Fact]
        public async Task GetByIdAsync_ExistingProduct_ReturnsIt()
        {
            Seed(NewProduct("Oat cakes", "Grosary"), NewProduct("Butter pack", "Milk food"));
            int id;
            using (var ctx = NewContext())
            {
                id = (await ctx.Product.SingleAsync(p => p.Title == "Butter pack")).Id;
            }

            using var context = NewContext();
            var result = await new ProductRepository(context).GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Butter pack", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            using var context = NewContext();

            var result = await new ProductRepository(context).GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrueOnlyForExistingProduct()
        {
            Seed(NewProduct("Cheese slice", "Milk food"));
            int id;
            using (var ctx = NewContext())
            {
                id = (await ctx.Product.SingleAsync()).Id;
            }

            using var context = NewContext();
            var repo = new ProductRepository(context);

            Assert.True(await repo.ExistsAsync(id));
            Assert.False(await repo.ExistsAsync(id + 100));
        }

        [Fact]
        public async Task GetPagedAsync_ReturnsRequestedPageWithPagingInfo()
        {
            Seed(
                NewProduct("Product A", "Cat1"),
                NewProduct("Product B", "Cat1"),
                NewProduct("Product C", "Cat1"),
                NewProduct("Product D", "Cat1"),
                NewProduct("Product E", "Cat1"));

            using var context = NewContext();
            var repo = new ProductRepository(context);

            var page1 = await repo.GetPagedAsync(null, 1, 3);
            var page2 = await repo.GetPagedAsync(null, 2, 3);

            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page1.TotalPages);
            Assert.False(page1.HasPreviousPage);
            Assert.True(page1.HasNextPage);

            Assert.Equal(2, page2.Count);
            Assert.True(page2.HasPreviousPage);
            Assert.False(page2.HasNextPage);
        }

        [Fact]
        public async Task GetPagedAsync_SearchByTitle_FiltersProducts()
        {
            Seed(
                NewProduct("Oat cakes", "Grosary"),
                NewProduct("Milk bottle", "Milk food"),
                NewProduct("Butter pack", "Milk food"));

            using var context = NewContext();
            var result = await new ProductRepository(context).GetPagedAsync("Oat", 1, 10);

            var item = Assert.Single(result);
            Assert.Equal("Oat cakes", item.Title);
        }

        [Fact]
        public async Task GetPagedAsync_SearchByCategory_FiltersProducts()
        {
            Seed(
                NewProduct("Oat cakes", "Grosary"),
                NewProduct("Milk bottle", "Milk food"),
                NewProduct("Butter pack", "Milk food"));

            using var context = NewContext();
            var result = await new ProductRepository(context).GetPagedAsync("Milk food", 1, 10);

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("Milk food", p.Category));
        }

        [Fact]
        public async Task GetPagedAsync_NothingFound_ReturnsEmptyList()
        {
            Seed(NewProduct("Oat cakes", "Grosary"));

            using var context = NewContext();
            var result = await new ProductRepository(context).GetPagedAsync("zzzz", 1, 10);

            Assert.Empty(result);
        }

        // ---------- Update ----------

        [Fact]
        public async Task UpdateAsync_PersistsChanges()
        {
            Seed(NewProduct("Old title", "Old category", 10m));
            Product toEdit;
            using (var ctx = NewContext())
            {
                toEdit = await new ProductRepository(ctx).GetByIdAsync((await ctx.Product.SingleAsync()).Id);
            }

            toEdit.Title = "New title";
            toEdit.Category = "New category";
            toEdit.Price = 99.99m;

            using (var context = NewContext())
            {
                await new ProductRepository(context).UpdateAsync(toEdit);
            }

            using var check = NewContext();
            var saved = await check.Product.SingleAsync();
            Assert.Equal("New title", saved.Title);
            Assert.Equal("New category", saved.Category);
            Assert.Equal(99.99m, saved.Price);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingProduct_ThrowsConcurrencyException()
        {
            var ghost = NewProduct("Ghost product", "Nothing");
            ghost.Id = 12345;

            using var context = NewContext();
            var repo = new ProductRepository(context);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repo.UpdateAsync(ghost));
        }

        // ---------- Delete ----------

        [Fact]
        public async Task DeleteAsync_RemovesOnlyThatProduct()
        {
            Seed(NewProduct("Keep me", "Cat"), NewProduct("Delete me", "Cat"));
            int idToDelete;
            using (var ctx = NewContext())
            {
                idToDelete = (await ctx.Product.SingleAsync(p => p.Title == "Delete me")).Id;
            }

            using (var context = NewContext())
            {
                await new ProductRepository(context).DeleteAsync(idToDelete);
            }

            using var check = NewContext();
            var left = await check.Product.SingleAsync();
            Assert.Equal("Keep me", left.Title);
        }

        [Fact]
        public async Task DeleteAsync_UnknownId_DoesNothing()
        {
            Seed(NewProduct("Keep me", "Cat"));

            using (var context = NewContext())
            {
                await new ProductRepository(context).DeleteAsync(999);
            }

            using var check = NewContext();
            Assert.Equal(1, await check.Product.CountAsync());
        }
    }
}
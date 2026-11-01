using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingSystemWeb.Controllers;
using ShoppingSystemWeb.Models;
using ShoppingSystemWeb.Repositories;
using ShoppingSystemWeb.Services;
using Xunit;

namespace ShoppingSystemWebTest.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductRepository> _repo = new Mock<IProductRepository>();
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _controller = new ProductsController(_repo.Object);
        }

        private static Product Sample(int id = 1) => new Product
        {
            Id = id,
            Title = "Sample product",
            Category = "Milk food",
            ExpiredDate = new DateTime(2030, 1, 1),
            Price = 10m
        };

        // ---------- Index ----------

        [Fact]
        public async Task Index_ReturnsViewWithPaginatedList()
        {
            var page = new PaginatedList<Product>(new List<Product> { Sample(1), Sample(2) }, 2, 2, 3);
            _repo.Setup(r => r.GetPagedAsync("milk", 2, 3)).ReturnsAsync(page);

            var result = await _controller.Index(2, "milk");

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PaginatedList<Product>>(view.Model);
            Assert.Equal(2, model.Count);
            _repo.Verify(r => r.GetPagedAsync("milk", 2, 3), Times.Once);
        }

        [Fact]
        public async Task Index_WithoutPageNumber_UsesFirstPage()
        {
            _repo.Setup(r => r.GetPagedAsync(null, 1, 3))
                 .ReturnsAsync(new PaginatedList<Product>(new List<Product>(), 0, 1, 3));

            var result = await _controller.Index(null, null);

            Assert.IsType<ViewResult>(result);
            _repo.Verify(r => r.GetPagedAsync(null, 1, 3), Times.Once);
        }

        // ---------- Details ----------

        [Fact]
        public async Task Details_NullId_ReturnsNotFound()
        {
            var result = await _controller.Details(null);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Details_UnknownId_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product)null);

            var result = await _controller.Details(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ExistingId_ReturnsViewWithProduct()
        {
            var product = Sample(5);
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(product);

            var result = await _controller.Details(5);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
        }

        // ---------- Create ----------

        [Fact]
        public void Create_Get_ReturnsView()
        {
            var result = _controller.Create();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_ValidModel_AddsProductAndRedirectsToIndex()
        {
            var product = Sample();
            _repo.Setup(r => r.AddAsync(product)).Returns(Task.CompletedTask);

            var result = await _controller.Create(product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _repo.Verify(r => r.AddAsync(product), Times.Once);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewAndDoesNotAdd()
        {
            var product = Sample();
            _controller.ModelState.AddModelError("Title", "Required");

            var result = await _controller.Create(product);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
            _repo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        // ---------- Edit ----------

        [Fact]
        public async Task Edit_Get_NullId_ReturnsNotFound()
        {
            var result = await _controller.Edit((int?)null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_UnknownId_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Product)null);

            var result = await _controller.Edit(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ExistingId_ReturnsViewWithProduct()
        {
            var product = Sample(3);
            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(product);

            var result = await _controller.Edit(3);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var result = await _controller.Edit(1, Sample(2));

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsViewAndDoesNotUpdate()
        {
            var product = Sample(1);
            _controller.ModelState.AddModelError("Price", "Out of range");

            var result = await _controller.Edit(1, product);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesAndRedirectsToIndex()
        {
            var product = Sample(1);
            _repo.Setup(r => r.UpdateAsync(product)).Returns(Task.CompletedTask);

            var result = await _controller.Edit(1, product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _repo.Verify(r => r.UpdateAsync(product), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyError_ProductDeleted_ReturnsNotFound()
        {
            var product = Sample(1);
            _repo.Setup(r => r.UpdateAsync(product)).ThrowsAsync(new DbUpdateConcurrencyException("conflict"));
            _repo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(false);

            var result = await _controller.Edit(1, product);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyError_ProductExists_Rethrows()
        {
            var product = Sample(1);
            _repo.Setup(r => r.UpdateAsync(product)).ThrowsAsync(new DbUpdateConcurrencyException("conflict"));
            _repo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _controller.Edit(1, product));
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_Get_NullId_ReturnsNotFound()
        {
            var result = await _controller.Delete(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_UnknownId_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((Product)null);

            var result = await _controller.Delete(7);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ExistingId_ReturnsViewWithProduct()
        {
            var product = Sample(7);
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(product);

            var result = await _controller.Delete(7);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesProductAndRedirectsToIndex()
        {
            _repo.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteConfirmed(7);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _repo.Verify(r => r.DeleteAsync(7), Times.Once);
        }
    }
}
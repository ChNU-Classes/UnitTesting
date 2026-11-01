using ShoppingSystemWeb.Services;
using Xunit;

namespace ShoppingSystemWebTest.Services
{
    public class PaginatedListTests
    {
        [Theory]
        [InlineData(0, 3, 0)]
        [InlineData(3, 3, 1)]
        [InlineData(7, 3, 3)]
        public void TotalPages_IsCalculatedFromCountAndPageSize(int count, int pageSize, int expectedPages)
        {
            var list = new PaginatedList<int>(new List<int>(), count, 1, pageSize);

            Assert.Equal(expectedPages, list.TotalPages);
        }

        [Fact]
        public void FirstPage_HasNextButNoPrevious()
        {
            var list = new PaginatedList<int>(new List<int> { 1, 2, 3 }, 7, 1, 3);

            Assert.False(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void MiddlePage_HasPreviousAndNext()
        {
            var list = new PaginatedList<int>(new List<int> { 4, 5, 6 }, 9, 2, 3);

            Assert.True(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
        }

        [Fact]
        public void LastPage_HasPreviousButNoNext()
        {
            var list = new PaginatedList<int>(new List<int> { 7 }, 7, 3, 3);

            Assert.True(list.HasPreviousPage);
            Assert.False(list.HasNextPage);
            Assert.Equal(3, list.PageIndex);
        }
    }
}

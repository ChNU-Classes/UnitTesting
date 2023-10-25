#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShoppingSystemWeb.Models;

namespace ShoppingSystemWeb.Data
{
    public class ShoppingSystemWebContext : DbContext
    {
		private DbContextOptionsBuilder<ShoppingSystemWebContext> option;

		public ShoppingSystemWebContext (DbContextOptions<ShoppingSystemWebContext> options)
            : base(options)
        {
        }

		public ShoppingSystemWebContext(DbContextOptionsBuilder<ShoppingSystemWebContext> option)
		{
			this.option = option;
		}

		public DbSet<ShoppingSystemWeb.Models.Product> Product { get; set; }
    }
}

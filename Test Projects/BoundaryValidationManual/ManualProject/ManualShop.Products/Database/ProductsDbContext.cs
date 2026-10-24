using Microsoft.EntityFrameworkCore;

namespace ManualShop.Products.Database;

public sealed class ProductsDbContext : DbContext
{
	public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
		: base(options)
	{
	}
}
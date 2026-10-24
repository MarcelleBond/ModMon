using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ManualShop.Products.Database;

public sealed class ProductsDbContextFactory
	: IDesignTimeDbContextFactory<ProductsDbContext>
{
	public ProductsDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("appsettings.Development.json", optional: true)
			.AddEnvironmentVariables()
			.AddCommandLine(args)
			.Build();

		var connectionString = configuration
			.GetConnectionString("DefaultConnection")
			?? string.Empty;

		var options = new DbContextOptionsBuilder<ProductsDbContext>();
		options.UseNpgsql(connectionString);
		return new ProductsDbContext(options.Options);
	}
}
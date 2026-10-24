using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ManualShop.Orders.Database;

public sealed class OrdersDbContextFactory
	: IDesignTimeDbContextFactory<OrdersDbContext>
{
	public OrdersDbContext CreateDbContext(string[] args)
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

		var options = new DbContextOptionsBuilder<OrdersDbContext>();
		options.UseNpgsql(connectionString);
		return new OrdersDbContext(options.Options);
	}
}
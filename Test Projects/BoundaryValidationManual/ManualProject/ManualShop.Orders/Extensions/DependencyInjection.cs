using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ManualShop.Orders.Database;

namespace ManualShop.Orders.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddOrdersDI(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration
			.GetConnectionString("DefaultConnection")
			?? string.Empty;

		services.AddDbContext<OrdersDbContext>(options =>
		{
			options.UseNpgsql(connectionString);
		});

		return services;
	}
}
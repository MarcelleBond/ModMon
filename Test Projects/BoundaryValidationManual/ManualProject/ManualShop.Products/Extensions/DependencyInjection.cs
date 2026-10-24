using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ManualShop.Products.Database;

namespace ManualShop.Products.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddProductsDI(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration
			.GetConnectionString("DefaultConnection")
			?? string.Empty;

		services.AddDbContext<ProductsDbContext>(options =>
		{
			options.UseNpgsql(connectionString);
		});

		return services;
	}
}
using ManualShop.SharedKernel;
using ManualShop.Orders.Extensions;
using OrdersDbUp = ManualShop.Orders.Database.Scripts.DbUpMigrator;
using ManualShop.Products.Extensions;
using ProductsDbUp = ManualShop.Products.Database.Scripts.DbUpMigrator;

namespace ManualShop.Api.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddProjectModules(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.AddSharedKernelDI(configuration);
		// <modules>
		services.AddProductsDI(configuration);
		services.AddOrdersDI(configuration);
		// </modules>
		return services;
	}

	public static WebApplication AddProjectDbUpMigrations(
		this WebApplication app,
		IConfiguration configuration)
	{
		// <dbup>
		ThrowIfFailed("Products", ProductsDbUp.TryMigrate(configuration));
		ThrowIfFailed("Orders", OrdersDbUp.TryMigrate(configuration));
		// </dbup>
		return app;
	}

	private static void ThrowIfFailed(
		string moduleName,
		int exitCode)
	{
		if (exitCode == 0)
		{
			return;
		}

		throw new InvalidOperationException(
			$"DbUp migration failed for module '{moduleName}'.");
	}
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ManualShop.SharedKernel.Events;

namespace ManualShop.SharedKernel;

public static class DependencyInjection
{
	public static IServiceCollection AddSharedKernelDI(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Register event dispatcher for in-process event handling
		services.AddScoped<IEventDispatcher, EventDispatcher>();

		return services;
	}
}
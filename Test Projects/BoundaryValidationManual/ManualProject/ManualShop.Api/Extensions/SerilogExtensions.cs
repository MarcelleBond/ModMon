using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ManualShop.Api.Extensions;

public static class SerilogExtensions
{
	public static void SerilogConfiguration(this ConfigureHostBuilder host)
	{
		host.UseSerilog((_, _, loggerConfiguration) =>
		{
			loggerConfiguration
				.MinimumLevel.Information()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.Console(new RenderedCompactJsonFormatter());
		});
	}
}
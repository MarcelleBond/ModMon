namespace ModMon.Cli;

internal static class SerilogApiExtensionsTemplates
{
	public static void WriteAll(
		IFileSystemWriter fileSystem,
		string apiRoot,
		string projectName)
	{
		fileSystem.WriteFile(
			Path.Combine(apiRoot, "Extensions", "SerilogExtensions.cs"),
			SerilogExtensions(projectName),
			overwrite: true);
	}

	private static string SerilogExtensions(string projectName)
	{
		return $$"""
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace {{projectName}}.Api.Extensions;

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
""";
	}
}

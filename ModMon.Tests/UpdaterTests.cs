using ModMon.Cli;

namespace ModMon.Tests;

public sealed class UpdaterTests
{
	private const string DiPath = @"C:\repo\Demo.Api\Extensions\DependencyInjection.cs";
	private const string DockerPath = @"C:\repo\Dockerfile";

	private readonly FakeFileSystemWriter _fs = new();

	[Fact]
	public void DiUpdaterRegistersModuleInsideMarkers()
	{
		_fs.WriteFile(DiPath, DiTemplate(), overwrite: false);
		var updater = new ApiDiAggregatorUpdater(_fs);

		var ok = updater.AddModule(DiPath, "Demo", "Orders");

		Assert.True(ok);
		var content = _fs.ReadFile(DiPath);
		Assert.Contains("services.AddOrdersDI(configuration);", content);
		Assert.Contains("OrdersDbUp", content);
		Assert.Contains(
			"MigrateDbContext<OrdersDbContext>(app);", content);
		Assert.Contains(
			"using Demo.Orders.Extensions;", content);
		Assert.Contains(
			"using Demo.Orders.Database;", content);
		Assert.Contains(
			"using Microsoft.EntityFrameworkCore;", content);
	}

	[Fact]
	public void DiUpdaterIsIdempotent()
	{
		_fs.WriteFile(DiPath, DiTemplate(), overwrite: false);
		var updater = new ApiDiAggregatorUpdater(_fs);
		updater.AddModule(DiPath, "Demo", "Orders");

		var again = updater.AddModule(DiPath, "Demo", "Orders");

		Assert.True(again);
		var content = _fs.ReadFile(DiPath);
		Assert.Equal(1, content.Split(
			"services.AddOrdersDI(configuration);").Length - 1);
		Assert.Equal(1, content.Split(
			"MigrateDbContext<OrdersDbContext>(app);").Length - 1);
	}

	[Fact]
	public void DiUpdaterReturnsFalseWhenFileMissing()
	{
		var updater = new ApiDiAggregatorUpdater(_fs);

		Assert.False(updater.AddModule(DiPath, "Demo", "Orders"));
	}

	[Fact]
	public void DiUpdaterReturnsFalseWhenMarkersMissing()
	{
		_fs.WriteFile(DiPath, "public static class X { }", overwrite: false);
		var updater = new ApiDiAggregatorUpdater(_fs);

		Assert.False(updater.AddModule(DiPath, "Demo", "Orders"));
	}

	[Fact]
	public void DockerUpdaterAddsCopyLineInsideMarkers()
	{
		_fs.WriteFile(DockerPath, DockerTemplate(), overwrite: false);
		var updater = new DockerfileModulesUpdater(_fs);

		var ok = updater.AddModuleProject(DockerPath, "Demo", "Orders");

		Assert.True(ok);
		var content = _fs.ReadFile(DockerPath);
		Assert.Contains(
			"COPY [\"Demo.Orders/Demo.Orders.csproj\", \"Demo.Orders/\"]",
			content);
		var start = content.IndexOf("# <modules>");
		var end = content.IndexOf("# </modules>");
		var copy = content.IndexOf("Demo.Orders.csproj");
		Assert.InRange(copy, start, end);
	}

	[Fact]
	public void DockerUpdaterIsIdempotent()
	{
		_fs.WriteFile(DockerPath, DockerTemplate(), overwrite: false);
		var updater = new DockerfileModulesUpdater(_fs);
		updater.AddModuleProject(DockerPath, "Demo", "Orders");

		Assert.True(updater.AddModuleProject(DockerPath, "Demo", "Orders"));
		Assert.Equal(1, _fs.ReadFile(DockerPath)
			.Split("Demo.Orders.csproj").Length - 1);
	}

	[Fact]
	public void DockerUpdaterReturnsFalseWhenFileOrMarkersMissing()
	{
		var updater = new DockerfileModulesUpdater(_fs);

		Assert.False(
			updater.AddModuleProject(DockerPath, "Demo", "Orders"));

		_fs.WriteFile(DockerPath, "FROM scratch\n", overwrite: false);
		Assert.False(
			updater.AddModuleProject(DockerPath, "Demo", "Orders"));
	}

	private static string DiTemplate()
	{
		return """
using Demo.SharedKernel;

namespace Demo.Api.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddProjectModules(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.AddSharedKernelDI(configuration);
		// <modules>
		// </modules>
		return services;
	}

	public static WebApplication ApplyProjectMigrations(
		this WebApplication app,
		IConfiguration configuration)
	{
		// <ef>
		// </ef>
		// <dbup>
		// </dbup>
		return app;
	}
}
""";
	}

	private static string DockerTemplate()
	{
		return """
FROM sdk AS build
COPY ["Demo.Api/Demo.Api.csproj", "Demo.Api/"]
COPY ["Demo.SharedKernel/Demo.SharedKernel.csproj", "Demo.SharedKernel/"]
# <modules>
# </modules>
RUN dotnet restore
""";
	}
}

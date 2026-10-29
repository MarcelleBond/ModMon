using ModMon.Cli;

namespace ModMon.Tests;

public sealed class SolutionDiscoveryTests : IDisposable
{
	private readonly string _root = Path.Combine(
		Path.GetTempPath(), $"modmon-sln-{Guid.NewGuid():N}");

	public SolutionDiscoveryTests()
	{
		Directory.CreateDirectory(_root);
	}

	public void Dispose()
	{
		Directory.Delete(_root, recursive: true);
	}

	[Fact]
	public void FindsSingleSolutionAndDerivesProjectName()
	{
		File.WriteAllText(Path.Combine(_root, "Demo.sln"), string.Empty);

		var lookup = new SolutionLocator().GetSolutionInfo(_root);

		Assert.Null(lookup.Error);
		Assert.Equal("Demo", lookup.Info!.ProjectName);
		Assert.Equal(_root, lookup.Info.RepoRoot);
	}

	[Fact]
	public void ReportsAmbiguityWhenMultipleSolutionsExist()
	{
		File.WriteAllText(Path.Combine(_root, "A.sln"), string.Empty);
		File.WriteAllText(Path.Combine(_root, "B.sln"), string.Empty);

		var lookup = new SolutionLocator().GetSolutionInfo(_root);

		Assert.Null(lookup.Info);
		Assert.Contains("Multiple solution files", lookup.Error);
	}

	[Fact]
	public void WalksUpFromNestedDirectories()
	{
		File.WriteAllText(Path.Combine(_root, "Demo.sln"), string.Empty);
		var nested = Path.Combine(_root, "a", "b", "c");
		Directory.CreateDirectory(nested);

		var lookup = new SolutionLocator().GetSolutionInfo(nested);

		Assert.Equal("Demo", lookup.Info!.ProjectName);
	}

	[Fact]
	public void ReturnsNullWhenNoSolutionAnywhere()
	{
		var isolated = Path.Combine(
			Path.GetTempPath(), $"modmon-empty-{Guid.NewGuid():N}");
		Directory.CreateDirectory(isolated);
		try
		{
			var lookup = new SolutionLocator().GetSolutionInfo(isolated);

			Assert.Null(lookup.Info);
			Assert.Null(lookup.Error);
		}
		finally
		{
			Directory.Delete(isolated, recursive: true);
		}
	}

	[Fact]
	public void WalksUpFromNonexistentStartPath()
	{
		File.WriteAllText(Path.Combine(_root, "Demo.sln"), string.Empty);
		var ghost = Path.Combine(_root, "gone", "deeper");

		var lookup = new SolutionLocator().GetSolutionInfo(ghost);

		Assert.Equal("Demo", lookup.Info!.ProjectName);
	}

	[Fact]
	public void ModuleScanExcludesApiKernelTestsAndMissingCsproj()
	{
		SeedSolution("Demo",
			("Demo.Api", true),
			("Demo.SharedKernel", true),
			("Demo.Orders", true),
			("Demo.Billing", true),
			("Demo.Orders.Tests", true),
			("Demo.Phantom", false),
			("Unrelated", true));

		var lookup = new ProjectInfoProvider(
			new PhysicalFileSystemWriter())
			.GetProjectInfo(_root);

		var names = lookup.Info!.Modules.Select(m => m.Name).ToList();
		Assert.Equal(new[] { "Billing", "Orders" }, names);
	}

	[Fact]
	public void ModuleScanReportsDbContextAndMigrationsFlags()
	{
		SeedSolution("Demo", ("Demo.Orders", true));
		var ordersDir = Path.Combine(_root, "Demo.Orders");
		var migrationsDir = Path.Combine(
			ordersDir, "Database", "Migrations");
		Directory.CreateDirectory(migrationsDir);
		File.WriteAllText(
			Path.Combine(ordersDir, "Database", "OrdersDbContext.cs"),
			"// ctx");
		File.WriteAllText(
			Path.Combine(migrationsDir, "OrdersDbContextModelSnapshot.cs"),
			"// snapshot");

		var lookup = new ProjectInfoProvider(
			new PhysicalFileSystemWriter())
			.GetProjectInfo(_root);

		var module = Assert.Single(lookup.Info!.Modules);
		Assert.True(module.HasDbContext);
		Assert.True(module.HasMigrations);
	}

	[Fact]
	public void ModuleScanReportsNoMigrationsWhenFolderIsEmpty()
	{
		SeedSolution("Demo", ("Demo.Orders", true));
		var ordersDir = Path.Combine(_root, "Demo.Orders");
		Directory.CreateDirectory(
			Path.Combine(ordersDir, "Database", "Migrations"));
		File.WriteAllText(
			Path.Combine(ordersDir, "Database", "OrdersDbContext.cs"),
			"// ctx");

		var lookup = new ProjectInfoProvider(
			new PhysicalFileSystemWriter())
			.GetProjectInfo(_root);

		var module = Assert.Single(lookup.Info!.Modules);
		Assert.True(module.HasDbContext);
		Assert.False(module.HasMigrations);
	}

	[Fact]
	public void ProviderPropagatesAmbiguityError()
	{
		File.WriteAllText(Path.Combine(_root, "A.sln"), string.Empty);
		File.WriteAllText(Path.Combine(_root, "B.sln"), string.Empty);

		var lookup = new ProjectInfoProvider(
			new PhysicalFileSystemWriter())
			.GetProjectInfo(_root);

		Assert.Null(lookup.Info);
		Assert.Contains("Multiple solution files", lookup.Error);
	}

	private void SeedSolution(
		string projectName,
		params (string Dir, bool WithCsproj)[] entries)
	{
		File.WriteAllText(
			Path.Combine(_root, $"{projectName}.sln"), string.Empty);
		foreach (var (dir, withCsproj) in entries)
		{
			var full = Path.Combine(_root, dir);
			Directory.CreateDirectory(full);
			if (withCsproj)
			{
				File.WriteAllText(
					Path.Combine(full, $"{dir}.csproj"), "<Project />");
			}
		}
	}
}

using ModMon.Cli;

namespace ModMon.Tests;

public sealed class TemplateTests
{
	[Fact]
	public void DockerfileUsesExistingChiseledTag()
	{
		var dockerfile = Templates.RootDockerfile("Demo");

		Assert.Contains(
			"mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled",
			dockerfile);
		Assert.DoesNotContain("10.0-chiseled ", dockerfile);
		Assert.DoesNotContain(
			"aspnet:10.0-chiseled\n", dockerfile);
	}

	[Fact]
	public void DockerfilePublishHasNoNoRestoreFlag()
	{
		var dockerfile = Templates.RootDockerfile("Demo");

		Assert.Contains("PublishReadyToRun=true", dockerfile);
		Assert.DoesNotContain("--no-restore", dockerfile);
		Assert.Contains("-a $TARGETARCH", dockerfile);
	}

	[Fact]
	public void DockerComposeLowercasesDockerNames()
	{
		var compose = Templates.DockerCompose("ECommerce");

		Assert.Contains("image: ecommerce-api:dev", compose);
		Assert.Contains("container_name: ecommerce-api", compose);
		Assert.Contains("container_name: ecommerce-db", compose);
		Assert.Contains("ecommerce_pgdata:", compose);
		Assert.DoesNotContain("ECommerce-api:dev", compose);
	}

	[Fact]
	public void DockerComposeKeepsPascalCaseForPostgresObjects()
	{
		var compose = Templates.DockerCompose("ECommerce");

		Assert.Contains("POSTGRES_DB: ECommerce", compose);
		Assert.Contains("POSTGRES_USER: ECommerce", compose);
	}

	[Fact]
	public void DockerComposeHealthchecksDbBeforeApi()
	{
		var compose = Templates.DockerCompose("Demo");

		Assert.Contains("condition: service_healthy", compose);
		Assert.Contains("pg_isready -U Demo -d Demo", compose);
		Assert.Contains("restart: unless-stopped", compose);
	}

	[Fact]
	public void DockerIgnoreExcludesBuildOutputs()
	{
		var ignore = Templates.DockerIgnore();

		Assert.Contains("**/bin/", ignore);
		Assert.Contains("**/obj/", ignore);
		Assert.Contains(".git/", ignore);
	}

	[Fact]
	public void ModuleMigratorEnsuresSchemaBeforeUpgrade()
	{
		var fs = new FakeFileSystemWriter();
		var root = @"C:\repo\Demo.Orders";

		ModuleTemplates.WriteAll(fs, root, "Demo", "Orders");

		var migrator = fs.ReadFile(Path.Combine(
			root, "Database", "Scripts", "DbUpMigrator.cs"));
		Assert.Contains("EnsureSchemaExists", migrator);
		Assert.Contains("orders", migrator);

		var init = fs.ReadFile(Path.Combine(
			root, "Database", "Scripts", "Versioned", "001_init.sql"));
		Assert.Contains("CREATE SCHEMA IF NOT EXISTS orders", init);
	}
}

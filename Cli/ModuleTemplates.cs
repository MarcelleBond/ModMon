namespace ModMon.Cli;

internal static class ModuleTemplates
{
	private const string ConnectionStringName = "DefaultConnection";

	public static void WriteAll(
		IFileSystemWriter fileSystem,
		string moduleRoot,
		string projectName,
		string moduleName)
	{
		var folders = new[]
		{
			"AutoMapperProfiles",
			Path.Combine("Database", "Migrations"),
			Path.Combine("Database", "Entities"),
			Path.Combine("Database", "Scripts", "Versioned"),
			Path.Combine("Database", "Scripts", "Repeatable"),
			"Controllers",
			"Extensions",
			"Exceptions",
			"Interfaces",
			"Models",
			"Services"
		};

		foreach (var folder in folders)
		{
			fileSystem.EnsureDirectory(Path.Combine(moduleRoot, folder));
		}

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Database", $"{moduleName}DbContext.cs"),
			ModuleDbContext(projectName, moduleName),
			overwrite: true);

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Extensions", "DependencyInjection.cs"),
			ModuleDependencyInjection(projectName, moduleName),
			overwrite: true);

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Database", $"{moduleName}DbContextFactory.cs"),
			ModuleDbContextFactory(projectName, moduleName),
			overwrite: true);

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Database", "Scripts", "Versioned", "001_init.sql"),
			"-- TODO: module versioned scripts (run once, in order)\n",
			overwrite: true);

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Database", "Scripts", "Repeatable", "R__views.sql"),
			"-- TODO: module repeatable scripts (rerun when changed)\n",
			overwrite: true);

		fileSystem.WriteFile(
			Path.Combine(moduleRoot, "Database", "Scripts", "DbUpMigrator.cs"),
			DbUpMigrator(projectName, moduleName),
			overwrite: true);
	}

	private static string DbUpMigrator(string projectName, string moduleName)
	{
		return $$"""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Configuration;

namespace {{projectName}}.{{moduleName}}.Database.Scripts;

public static class DbUpMigrator
{
	public static int TryMigrate(
		IConfiguration configuration,
		string? _ = null)
	{
		var connectionString = GetConnectionString(configuration);
		var upgrader = BuildUpgrader(connectionString);
		var result = upgrader.PerformUpgrade();
		return result.Successful ? 0 : 1;
	}

	private static string GetConnectionString(IConfiguration configuration)
	{
		var conn = configuration.GetConnectionString("{{ConnectionStringName}}");
		return conn ?? string.Empty;
	}

	private static UpgradeEngine BuildUpgrader(
		string connectionString)
	{
		var assembly = typeof(DbUpMigrator).Assembly;
		var versioned = GetVersionedScriptNames(assembly);
		var repeatable = GetRepeatableScriptNames(assembly);
		var journalSchema = "{{moduleName.ToLowerInvariant()}}";
		var journalTable = "schema_versions";

		return DeployChanges.To
			.PostgresqlDatabase(connectionString)
			.WithTransaction()
			.JournalToPostgresqlTable(journalSchema, journalTable)
			.WithVariablesDisabled()
			.WithScriptsEmbeddedInAssembly(assembly, x => versioned.Contains(x))
			.WithScriptsEmbeddedInAssembly(assembly, x => repeatable.Contains(x))
			.LogToConsole()
			.Build();
	}

	private static HashSet<string> GetVersionedScriptNames(Assembly assembly)
	{
		return assembly.GetManifestResourceNames()
			.Where(x => x.Contains(".Database.Scripts.Versioned."))
			.ToHashSet(StringComparer.Ordinal);
	}

	private static HashSet<string> GetRepeatableScriptNames(Assembly assembly)
	{
		return assembly.GetManifestResourceNames()
			.Where(x => x.Contains(".Database.Scripts.Repeatable."))
			.ToHashSet(StringComparer.Ordinal);
	}
}
""";
	}

	private static string ModuleDbContext(string projectName, string moduleName)
	{
		return $$"""
using Microsoft.EntityFrameworkCore;

namespace {{projectName}}.{{moduleName}}.Database;

public sealed class {{moduleName}}DbContext : DbContext
{
	public {{moduleName}}DbContext(DbContextOptions<{{moduleName}}DbContext> options)
		: base(options)
	{
	}
}
""";
	}

	private static string ModuleDbContextFactory(string projectName, string moduleName)
	{
		return $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace {{projectName}}.{{moduleName}}.Database;

public sealed class {{moduleName}}DbContextFactory
	: IDesignTimeDbContextFactory<{{moduleName}}DbContext>
{
	public {{moduleName}}DbContext CreateDbContext(string[] args)
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

		var options = new DbContextOptionsBuilder<{{moduleName}}DbContext>();
		options.UseNpgsql(connectionString);
		return new {{moduleName}}DbContext(options.Options);
	}
}
""";
	}

	private static string ModuleDependencyInjection(
		string projectName,
		string moduleName)
	{
		return $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{projectName}}.{{moduleName}}.Database;

namespace {{projectName}}.{{moduleName}}.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection Add{{moduleName}}DI(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration
			.GetConnectionString("DefaultConnection")
			?? string.Empty;

		services.AddDbContext<{{moduleName}}DbContext>(options =>
		{
			options.UseNpgsql(connectionString);
		});

		return services;
	}
}
""";
	}
}

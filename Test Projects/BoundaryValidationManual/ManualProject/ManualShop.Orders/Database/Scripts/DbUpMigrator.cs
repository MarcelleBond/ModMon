using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Configuration;

namespace ManualShop.Orders.Database.Scripts;

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
		var conn = configuration.GetConnectionString("DefaultConnection");
		return conn ?? string.Empty;
	}

	private static UpgradeEngine BuildUpgrader(
		string connectionString)
	{
		var assembly = typeof(DbUpMigrator).Assembly;
		var versioned = GetVersionedScriptNames(assembly);
		var repeatable = GetRepeatableScriptNames(assembly);
		var journalSchema = "orders";
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
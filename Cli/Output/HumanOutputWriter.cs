using System.Text.Json;

namespace ModMon.Cli;

internal sealed class HumanOutputWriter : IOutputWriter
{
	private readonly IConsole _console;

	public HumanOutputWriter(IConsole console)
	{
		_console = console;
	}

	public void WriteSuccess(object data)
	{
		switch (data)
		{
			case InitResult result:
				WriteInit(result);
				return;
			case ModuleAddResult result:
				WriteModuleAdd(result);
				return;
			case InfoResult result:
				WriteInfo(result);
				return;
			case ModuleListResult result:
				WriteModuleList(result);
				return;
			case ValidateResult result:
				WriteValidate(result);
				return;
			case string message:
				_console.WriteLine(message);
				return;
		}

		var json = JsonSerializer.Serialize(
			data,
			new JsonSerializerOptions { WriteIndented = true });
		_console.WriteLine(json);
	}

	public void WriteError(string message, object? details = null)
	{
		_console.WriteError(message);
		if (details is not CommandResult result || result.Errors == null)
		{
			return;
		}

		foreach (var error in result.Errors)
		{
			if (!string.Equals(error, message, StringComparison.Ordinal))
			{
				_console.WriteError($"  - {error}");
			}
		}
	}

	public void WriteProgress(string message)
	{
		_console.WriteLine(message);
	}

	public void Flush()
	{
	}

	private void WriteInit(InitResult result)
	{
		var header = result.DryRun
			? $"Dry run for project '{result.ProjectName}' — no changes made."
			: $"Project '{result.ProjectName}' initialized successfully.";
		_console.WriteLine(header);
		WriteField("Solution", result.SolutionPath);
		WriteList("Projects", result.CreatedProjects);
		WriteList("Created", result.CreatedFiles);
		WriteList("Updated", result.UpdatedFiles);
		WriteList("Preserved", result.SkippedFiles);
		WriteOperations(result.Operations);
		WriteWarnings(result.Warnings);
	}

	private void WriteModuleAdd(ModuleAddResult result)
	{
		var header = result.DryRun
			? $"Dry run for module '{result.ModuleName}' — no changes made."
			: $"Module '{result.ModuleName}' added successfully.";
		_console.WriteLine(header);
		WriteField("Path", result.ModulePath);
		var migration = result.DryRun
			? "would create"
			: result.MigrationCreated
				? "created"
				: "not created";
		WriteField("EF migration", migration);
		WriteList("Created", result.CreatedFiles);
		WriteList("Updated", result.UpdatedFiles);
		WriteOperations(result.Operations);
		WriteWarnings(result.Warnings);
	}

	private void WriteInfo(InfoResult result)
	{
		_console.WriteLine($"Project: {result.ProjectName}");
		WriteField("Solution", result.SolutionPath);
		WriteField("Root", result.RepoRoot);
		WriteList("Projects", result.Projects);
		var modules = result.Modules ?? new List<ModuleInfo>();
		_console.WriteLine($"  Modules ({modules.Count}):");
		WriteModules(modules);
	}

	private void WriteModuleList(ModuleListResult result)
	{
		_console.WriteLine(
			$"{result.ProjectName}: {result.Count} module(s)");
		WriteModules(result.Modules);
	}

	private void WriteValidate(ValidateResult result)
	{
		var state = result.Valid ? "VALID" : "INVALID";
		_console.WriteLine($"Project '{result.ProjectName}': {state}");
		foreach (var check in result.Checks ?? new List<ValidationCheck>())
		{
			var tag = check.Passed ? "[PASS]" : "[FAIL]";
			var detail = check.Passed || check.Message == null
				? string.Empty
				: $" — {check.Message}";
			_console.WriteLine($"  {tag} {check.Name}{detail}");
		}
	}

	private void WriteModules(List<ModuleInfo>? modules)
	{
		foreach (var module in modules ?? new List<ModuleInfo>())
		{
			var db = module.HasDbContext ? "yes" : "no";
			var migrations = module.HasMigrations ? "yes" : "no";
			_console.WriteLine(
				$"  {module.Name} " +
				$"(DbContext: {db}, Migrations: {migrations})");
		}
	}

	private void WriteField(string label, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			_console.WriteLine($"  {label}: {value}");
		}
	}

	private void WriteList(string label, List<string>? items)
	{
		if (items == null || items.Count == 0)
		{
			return;
		}

		_console.WriteLine($"  {label} ({items.Count}):");
		foreach (var item in items)
		{
			_console.WriteLine($"    {item}");
		}
	}

	private void WriteOperations(List<DryRunOperation>? operations)
	{
		if (operations == null || operations.Count == 0)
		{
			return;
		}

		_console.WriteLine($"  Operations ({operations.Count}):");
		foreach (var operation in operations)
		{
			_console.WriteLine(
				$"    {operation.Type}: {Describe(operation)}");
		}
	}

	private static string Describe(DryRunOperation operation)
	{
		foreach (var key in new[] { "path", "command", "reason" })
		{
			if (operation.Details.TryGetValue(key, out var value))
			{
				return value?.ToString() ?? string.Empty;
			}
		}

		return string.Empty;
	}

	private void WriteWarnings(List<string>? warnings)
	{
		foreach (var warning in warnings ?? new List<string>())
		{
			_console.WriteLine($"  warning: {warning}");
		}
	}
}

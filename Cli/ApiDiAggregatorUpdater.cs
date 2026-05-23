namespace ModMon.Cli;

internal sealed class ApiDiAggregatorUpdater
{
	private const string MarkerStart = "// <modules>";
	private const string MarkerEnd = "// </modules>";
	private const string DbUpMarkerStart = "// <dbup>";
	private const string DbUpMarkerEnd = "// </dbup>";
	private readonly IFileSystemWriter _fileSystem;

	public ApiDiAggregatorUpdater(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public void AddModule(string filePath, string projectName, string moduleName)
	{
		if (!_fileSystem.FileExists(filePath))
		{
			return;
		}

		var content = File.ReadAllText(filePath);
		var newLine = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";
		content = AddUsingIfMissing(content, projectName, moduleName);
		content = AddDbUpAliasIfMissing(content, projectName, moduleName);

		var start = content.IndexOf(MarkerStart, StringComparison.Ordinal);
		var end = content.IndexOf(MarkerEnd, StringComparison.Ordinal);
		if (start < 0 || end < 0 || end <= start)
		{
			return;
		}

		var insertionPoint = start + MarkerStart.Length;
		var toInsert =
			$"{newLine}\t\tservices.Add{moduleName}DI(configuration);";
		if (content.Contains(
			$"services.Add{moduleName}DI(configuration);",
			StringComparison.Ordinal))
		{
			return;
		}

		var updated = content.Insert(insertionPoint, toInsert);
		updated = AddDbUpIfMissing(
			updated,
			projectName,
			moduleName,
			newLine);
		_fileSystem.WriteFile(filePath, updated, overwrite: true);
	}

	private static string AddDbUpIfMissing(
		string content,
		string projectName,
		string moduleName,
		string newLine)
	{
		var start = content.IndexOf(DbUpMarkerStart, StringComparison.Ordinal);
		var end = content.IndexOf(DbUpMarkerEnd, StringComparison.Ordinal);
		if (start < 0 || end < 0 || end <= start)
		{
			return content;
		}

		var alias = $"{moduleName}DbUp";
		var call =
			$"ThrowIfFailed(\"{moduleName}\", " +
			$"{alias}.TryMigrate(configuration));";
		if (content.Contains(call, StringComparison.Ordinal))
		{
			return content;
		}

		var insertionPoint = start + DbUpMarkerStart.Length;
		var toInsert = $"{newLine}\t\t{call}";
		return content.Insert(insertionPoint, toInsert);
	}

	private static string AddDbUpAliasIfMissing(
		string content,
		string projectName,
		string moduleName)
	{
		var aliasLine =
			$"using {moduleName}DbUp = {projectName}.{moduleName}." +
			"Database.Scripts.DbUpMigrator;";
		if (content.Contains(aliasLine, StringComparison.Ordinal))
		{
			return content;
		}

		var newLine = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";
		var lines = content.Split(newLine);
		var insertAt = 0;
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].StartsWith("using ", StringComparison.Ordinal))
			{
				insertAt = i + 1;
				continue;
			}

			break;
		}

		var list = lines.ToList();
		list.Insert(insertAt, aliasLine);
		return string.Join(newLine, list);
	}

	private static string AddUsingIfMissing(
		string content,
		string projectName,
		string moduleName)
	{
		var usingLine = $"using {projectName}.{moduleName}.Extensions;";
		if (content.Contains(usingLine, StringComparison.Ordinal))
		{
			return content;
		}

		var newLine = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";

		var lines = content.Split(newLine);
		var insertAt = 0;
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].StartsWith("using ", StringComparison.Ordinal))
			{
				insertAt = i + 1;
				continue;
			}

			break;
		}

		var list = lines.ToList();
		list.Insert(insertAt, usingLine);
		return string.Join(newLine, list);
	}
}

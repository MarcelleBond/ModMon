namespace ModMon.Cli;

internal sealed class DockerfileModulesUpdater
{
	private const string MarkerStart = "# <modules>";
	private const string MarkerEnd = "# </modules>";
	private readonly IFileSystemWriter _fileSystem;

	public DockerfileModulesUpdater(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public void AddModuleProject(
		string dockerfilePath,
		string projectName,
		string moduleName)
	{
		if (!_fileSystem.FileExists(dockerfilePath))
		{
			return;
		}

		var content = File.ReadAllText(dockerfilePath);
		var newLine = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";

		var moduleProject = $"{projectName}.{moduleName}";
		var copyLine =
			$"COPY [\"{moduleProject}/{moduleProject}.csproj\", \"{moduleProject}/\"]";
		if (content.Contains(copyLine, StringComparison.Ordinal))
		{
			return;
		}

		var start = content.IndexOf(MarkerStart, StringComparison.Ordinal);
		var end = content.IndexOf(MarkerEnd, StringComparison.Ordinal);
		if (start < 0 || end < 0 || end <= start)
		{
			return;
		}

		var insertionPoint = start + MarkerStart.Length;
		var updated = content.Insert(insertionPoint, newLine + copyLine);
		_fileSystem.WriteFile(dockerfilePath, updated, overwrite: true);
	}
}

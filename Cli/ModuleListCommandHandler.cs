namespace ModMon.Cli;

internal sealed class ModuleListCommandHandler
{
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;

	public ModuleListCommandHandler(
		IFileSystemWriter fileSystem,
		IOutputWriter output)
	{
		_fileSystem = fileSystem;
		_output = output;
	}

	public int Handle(string? path)
	{
		var startDirectory = string.IsNullOrWhiteSpace(path)
			? Directory.GetCurrentDirectory()
			: Path.GetFullPath(path);

		var provider = new ProjectInfoProvider(_fileSystem);
		var projectInfo = provider.GetProjectInfo(startDirectory);

		if (projectInfo == null)
		{
			var errorResult = new ModuleListResult(
				Success: false,
				Message: "No project found",
				ExitCode: ExitCode.ResourceNotFound,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					"Unable to find a .sln file in the directory tree"
				});
			_output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.ResourceNotFound;
		}

		var result = new ModuleListResult(
			Success: true,
			Message: $"Found {projectInfo.Modules.Count} module(s)",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: projectInfo.ProjectName,
			Modules: projectInfo.Modules,
			Count: projectInfo.Modules.Count);

		_output.WriteSuccess(result);
		return (int)ExitCode.Success;
	}
}

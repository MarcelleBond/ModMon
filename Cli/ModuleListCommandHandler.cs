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
			: PathResolver.TryGetFullPath(path);
		if (startDirectory == null)
		{
			var invalid = new ModuleListResult(
				Success: false,
				Message: $"Invalid --path '{path}'.",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { $"Invalid --path '{path}'." });
			_output.WriteError(invalid.Message, invalid);
			return (int)invalid.ExitCode;
		}

		var provider = new ProjectInfoProvider(_fileSystem);
		var lookup = provider.GetProjectInfo(startDirectory);

		if (lookup.Info == null)
		{
			var message = lookup.Error
				?? "Unable to find a .sln file in the directory tree";
			var exitCode = lookup.Error != null
				? ExitCode.ValidationFailed
				: ExitCode.ResourceNotFound;
			var errorResult = new ModuleListResult(
				Success: false,
				Message: message,
				ExitCode: exitCode,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { message });
			_output.WriteError(errorResult.Message, errorResult);
			return (int)exitCode;
		}

		var projectInfo = lookup.Info;

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

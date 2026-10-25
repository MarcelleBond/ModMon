namespace ModMon.Cli;

internal sealed class InfoCommandHandler
{
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;

	public InfoCommandHandler(
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
			var invalid = new InfoResult(
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
			var errorResult = new InfoResult(
				Success: false,
				Message: message,
				ExitCode: exitCode,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { message });
			_output.WriteError(errorResult.Message, errorResult);
			return (int)exitCode;
		}

		var projectInfo = lookup.Info;

		var result = new InfoResult(
			Success: true,
			Message: "Project information retrieved successfully",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: projectInfo.ProjectName,
			SolutionPath: projectInfo.SolutionPath,
			RepoRoot: projectInfo.RepoRoot,
			Modules: projectInfo.Modules,
			Projects: projectInfo.Projects);

		_output.WriteSuccess(result);
		return (int)ExitCode.Success;
	}
}

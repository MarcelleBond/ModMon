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
			: Path.GetFullPath(path);

		var provider = new ProjectInfoProvider(_fileSystem);
		var projectInfo = provider.GetProjectInfo(startDirectory);

		if (projectInfo == null)
		{
			var errorResult = new InfoResult(
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

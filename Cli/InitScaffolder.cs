namespace ModMon.Cli;

internal sealed class InitScaffolder
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;

	public InitScaffolder(
		IConsole console,
		IDotnetCliRunner dotnet,
		IFileSystemWriter fileSystem)
	{
		_console = console;
		_dotnet = dotnet;
		_fileSystem = fileSystem;
	}

	public async Task<InitResult> ScaffoldAsync(
		string repoRoot,
		string projectName,
		IDryRunTracker tracker)
	{
		var apiProject = $"{projectName}.Api";
		var kernelProject = $"{projectName}.SharedKernel";
		var createdProjects = new List<string>();
		var createdFiles = new List<string>();

		var steps = new DotnetInitSteps(_dotnet, _console);
		var result = await steps.RunAsync(
			repoRoot,
			projectName,
			apiProject,
			kernelProject);
		if (result != 0)
		{
			return new InitResult(
				Success: false,
				Message: "Failed to initialize project structure",
				ExitCode: ExitCode.ExternalToolError,
				Timestamp: DateTime.UtcNow,
				ProjectName: projectName,
				RepoRoot: repoRoot,
				Errors: new List<string> { "dotnet CLI commands failed" });
		}

		createdProjects.Add(apiProject);
		createdProjects.Add(kernelProject);

		var writer = new TemplateWriter(_fileSystem);
		writer.WriteApiBaseline(repoRoot, projectName);
		writer.WriteRootDockerFiles(repoRoot, projectName);
		writer.WriteSharedKernelTemplates(repoRoot, projectName);
		writer.WriteApiDiAggregator(repoRoot, projectName);

		var solutionPath = Path.Combine(repoRoot, $"{projectName}.sln");

		var message = tracker.IsDryRun
			? "Dry run completed. No changes made."
			: "Project initialized successfully";

		return new InitResult(
			Success: true,
			Message: message,
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: projectName,
			SolutionPath: solutionPath,
			RepoRoot: repoRoot,
			CreatedProjects: createdProjects,
			CreatedFiles: createdFiles,
			DryRun: tracker.IsDryRun,
			Operations: tracker.IsDryRun ? tracker.GetOperations() : null);
	}
}

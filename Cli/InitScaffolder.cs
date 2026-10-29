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
		var artifacts = new[]
		{
			Path.Combine(repoRoot, $"{projectName}.sln"),
			Path.Combine(repoRoot, apiProject),
			Path.Combine(repoRoot, kernelProject),
			Path.Combine(repoRoot, "Dockerfile"),
			Path.Combine(repoRoot, "docker-compose.yml"),
			Path.Combine(repoRoot, ".dockerignore"),
			Path.Combine(repoRoot, ".gitignore"),
			Path.Combine(repoRoot, "AGENTS.md"),
			Path.Combine(repoRoot, ".agents", "skills", "modmon")
		};
		var preExisting = artifacts.Where(PathExists).ToList();

		try
		{
			return await RunPipelineAsync(
				repoRoot,
				projectName,
				tracker,
				artifacts,
				preExisting);
		}
		catch (Exception exception)
		{
			_console.WriteError($"Scaffolding failed: {exception.Message}");
			CleanupArtifacts(artifacts, preExisting);
			return new InitResult(
				Success: false,
				Message: "Failed to initialize project",
				ExitCode: ExitCode.GeneralFailure,
				Timestamp: DateTime.UtcNow,
				ProjectName: projectName,
				RepoRoot: repoRoot,
				Errors: new List<string> { exception.Message });
		}
	}

	private async Task<InitResult> RunPipelineAsync(
		string repoRoot,
		string projectName,
		IDryRunTracker tracker,
		string[] artifacts,
		List<string> preExisting)
	{
		var apiProject = $"{projectName}.Api";
		var kernelProject = $"{projectName}.SharedKernel";
		var writes = new TrackingFileSystemWriter(_fileSystem);

		var steps = new DotnetInitSteps(_dotnet, _console, writes);
		var result = await steps.RunAsync(
			repoRoot,
			projectName,
			apiProject,
			kernelProject);
		if (result != 0)
		{
			CleanupArtifacts(artifacts, preExisting);
			return new InitResult(
				Success: false,
				Message: "Failed to initialize project structure",
				ExitCode: ExitCode.ExternalToolError,
				Timestamp: DateTime.UtcNow,
				ProjectName: projectName,
				RepoRoot: repoRoot,
				Errors: new List<string> { "dotnet CLI commands failed" });
		}

		var writer = new TemplateWriter(writes);
		writer.WriteApiBaseline(repoRoot, projectName);
		writer.WriteRootDockerFiles(repoRoot, projectName);
		writer.WriteGitIgnore(repoRoot);
		writer.WriteSharedKernelTemplates(repoRoot, projectName);
		writer.WriteApiDiAggregator(repoRoot, projectName);
		var guidance = new AgentGuidanceWriter(writes)
			.WriteAll(repoRoot, projectName, tracker);

		var createdFiles = writes.Created
			.Concat(guidance.CreatedFiles)
			.Distinct()
			.ToList();
		var createdProjects = new List<string> { apiProject, kernelProject };
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
			UpdatedFiles: writes.Updated,
			SkippedFiles: guidance.SkippedFiles,
			DryRun: tracker.IsDryRun,
			Operations: tracker.IsDryRun ? tracker.GetOperations() : null);
	}

	private bool PathExists(string path)
	{
		return _fileSystem.FileExists(path)
			|| _fileSystem.DirectoryExists(path);
	}

	private void CleanupArtifacts(
		string[] artifacts,
		List<string> preExisting)
	{
		foreach (var path in artifacts.Except(preExisting))
		{
			TryDeletePath(path);
		}
	}

	private void TryDeletePath(string path)
	{
		try
		{
			if (_fileSystem.DirectoryExists(path))
			{
				_fileSystem.DeleteDirectory(path);
			}
			else
			{
				_fileSystem.DeleteFile(path);
			}
		}
		catch (Exception ex)
		{
			_console.WriteError(
				$"Cleanup: unable to remove '{path}': {ex.Message}");
		}
	}
}

namespace ModMon.Cli;

internal sealed class ModuleScaffolder
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;

	public ModuleScaffolder(
		IConsole console,
		IDotnetCliRunner dotnet,
		IFileSystemWriter fileSystem)
	{
		_console = console;
		_dotnet = dotnet;
		_fileSystem = fileSystem;
	}

	public async Task<ModuleAddResult> ScaffoldAsync(
		string repoRoot,
		ModuleAddRequest request,
		IDryRunTracker tracker)
	{
		var moduleProject = $"{request.ProjectName}.{request.ModuleName}";
		var apiProject = $"{request.ProjectName}.Api";
		var kernelProject = $"{request.ProjectName}.SharedKernel";
		var createdFiles = new List<string>();
		var updatedFiles = new List<string>();

		var moduleFolder = Path.Combine(repoRoot, moduleProject);
		if (_fileSystem.DirectoryExists(moduleFolder))
		{
			return new ModuleAddResult(
				Success: false,
				Message: "Module already exists",
				ExitCode: ExitCode.ResourceAlreadyExists,
				Timestamp: DateTime.UtcNow,
				ModuleName: request.ModuleName,
				ModulePath: moduleFolder,
				Errors: new List<string>
				{
					$"Module directory already exists: {moduleFolder}"
				});
		}

		var steps = new DotnetModuleSteps(_dotnet, _console);
		var exit = await steps.RunAsync(
			repoRoot,
			request.ProjectName,
			apiProject,
			kernelProject,
			moduleProject);
		if (exit != 0)
		{
			return new ModuleAddResult(
				Success: false,
				Message: "Failed to create module project",
				ExitCode: ExitCode.ExternalToolError,
				Timestamp: DateTime.UtcNow,
				ModuleName: request.ModuleName,
				Errors: new List<string> { "dotnet CLI commands failed" });
		}

		var writer = new TemplateWriter(_fileSystem);
		writer.WriteModuleTemplates(
			repoRoot,
			request.ProjectName,
			request.ModuleName);
		writer.UpdateApiDiAggregator(
			repoRoot,
			request.ProjectName,
			request.ModuleName);
		writer.UpdateRootDockerFiles(
			repoRoot,
			request.ProjectName,
			request.ModuleName);

		var diAggregatorPath = Path.Combine(
			repoRoot,
			$"{request.ProjectName}.Api",
			"Extensions",
			"DependencyInjection.cs");
		updatedFiles.Add(diAggregatorPath);

		var dockerfilePath = Path.Combine(repoRoot, "Dockerfile");
		updatedFiles.Add(dockerfilePath);

		var ef = new EfMigrationSteps(_dotnet, _console);
		var migrationResult = await ef.TryCreateInitialMigrationAsync(
			repoRoot,
			request.ProjectName,
			moduleProject);

		var message = tracker.IsDryRun
			? "Dry run completed. No changes made."
			: "Module added successfully";

		return new ModuleAddResult(
			Success: true,
			Message: message,
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ModuleName: request.ModuleName,
			ModulePath: moduleFolder,
			MigrationCreated: migrationResult == 0,
			UpdatedFiles: updatedFiles,
			CreatedFiles: createdFiles,
			DryRun: tracker.IsDryRun,
			Operations: tracker.IsDryRun ? tracker.GetOperations() : null);
	}
}

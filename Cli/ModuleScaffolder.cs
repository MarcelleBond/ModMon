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
		var context = new ModulePipelineContext(
			repoRoot,
			$"{request.ProjectName}.{request.ModuleName}",
			Path.Combine(
				repoRoot,
				$"{request.ProjectName}.{request.ModuleName}"),
			SnapshotFiles(repoRoot, request.ProjectName));
		if (_fileSystem.DirectoryExists(context.ModuleFolder))
		{
			return new ModuleAddResult(
				Success: false,
				Message: "Module already exists",
				ExitCode: ExitCode.ResourceAlreadyExists,
				Timestamp: DateTime.UtcNow,
				ModuleName: request.ModuleName,
				ModulePath: context.ModuleFolder,
				Errors: new List<string>
				{
					$"Module directory already exists: {context.ModuleFolder}"
				});
		}

		try
		{
			return await RunPipelineAsync(request, tracker, context);
		}
		catch (Exception exception)
		{
			_console.WriteError($"Scaffolding failed: {exception.Message}");
			await CleanupFailedModuleAsync(request, context);
			return new ModuleAddResult(
				Success: false,
				Message: "Failed to add module",
				ExitCode: ExitCode.GeneralFailure,
				Timestamp: DateTime.UtcNow,
				ModuleName: request.ModuleName,
				ModulePath: context.ModuleFolder,
				Errors: new List<string> { exception.Message });
		}
	}

	private async Task<ModuleAddResult> RunPipelineAsync(
		ModuleAddRequest request,
		IDryRunTracker tracker,
		ModulePipelineContext context)
	{
		var apiProject = $"{request.ProjectName}.Api";
		var kernelProject = $"{request.ProjectName}.SharedKernel";
		var writes = new TrackingFileSystemWriter(_fileSystem);
		var steps = new DotnetModuleSteps(_dotnet, _console, writes);
		var exit = await steps.RunAsync(
			context.RepoRoot,
			request.ProjectName,
			apiProject,
			kernelProject,
			context.ModuleProject);
		if (exit != 0)
		{
			await CleanupFailedModuleAsync(request, context);
			return new ModuleAddResult(
				Success: false,
				Message: "Failed to create module project",
				ExitCode: ExitCode.ExternalToolError,
				Timestamp: DateTime.UtcNow,
				ModuleName: request.ModuleName,
				Errors: new List<string> { "dotnet CLI commands failed" });
		}

		var writer = new TemplateWriter(writes);
		writer.WriteModuleTemplates(
			context.RepoRoot,
			request.ProjectName,
			request.ModuleName);
		var diUpdated = writer.UpdateApiDiAggregator(
			context.RepoRoot,
			request.ProjectName,
			request.ModuleName);
		var dockerUpdated = writer.UpdateRootDockerFiles(
			context.RepoRoot,
			request.ProjectName,
			request.ModuleName);

		var ef = new EfMigrationSteps(_dotnet, _console);
		var migrationResult = await ef.TryCreateInitialMigrationAsync(
			context.RepoRoot,
			request.ProjectName,
			context.ModuleProject);

		var outcome = new ModuleWriteOutcome(
			diUpdated,
			dockerUpdated,
			migrationResult);
		return CreateSuccessResult(request, tracker, context, writes, outcome);
	}

	private static ModuleAddResult CreateSuccessResult(
		ModuleAddRequest request,
		IDryRunTracker tracker,
		ModulePipelineContext context,
		TrackingFileSystemWriter writes,
		ModuleWriteOutcome outcome)
	{
		var message = tracker.IsDryRun
			? "Dry run completed. No changes made."
			: "Module added successfully";

		return new ModuleAddResult(
			Success: true,
			Message: message,
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ModuleName: request.ModuleName,
			ModulePath: context.ModuleFolder,
			MigrationCreated: outcome.MigrationExitCode == 0,
			UpdatedFiles: writes.Updated,
			CreatedFiles: writes.Created,
			DryRun: tracker.IsDryRun,
			Operations: tracker.IsDryRun ? tracker.GetOperations() : null,
			Warnings: BuildWarnings(request.ModuleName, outcome));
	}

	private static List<string>? BuildWarnings(
		string moduleName,
		ModuleWriteOutcome outcome)
	{
		var warnings = new List<string>();
		if (!outcome.DiAggregatorUpdated)
		{
			warnings.Add(
				"API DI aggregator missing or has no markers; " +
				$"module '{moduleName}' was not registered.");
		}
		if (!outcome.DockerfileUpdated)
		{
			warnings.Add(
				"Dockerfile missing or has no markers; " +
				$"module '{moduleName}' was not added to the image build.");
		}
		if (outcome.MigrationExitCode != 0)
		{
			warnings.Add(
				"EF Core initial migration was not created; " +
				"see command output for details.");
		}

		return warnings.Count == 0 ? null : warnings;
	}

	private Dictionary<string, string> SnapshotFiles(
		string repoRoot,
		string projectName)
	{
		var files = new[]
		{
			Path.Combine(
				repoRoot,
				$"{projectName}.Api",
				"Extensions",
				"DependencyInjection.cs"),
			Path.Combine(repoRoot, "Dockerfile")
		};
		var snapshot = new Dictionary<string, string>(
			StringComparer.OrdinalIgnoreCase);
		foreach (var file in files)
		{
			if (_fileSystem.FileExists(file))
			{
				snapshot[file] = _fileSystem.ReadFile(file);
			}
		}

		return snapshot;
	}

	private void RestoreFiles(Dictionary<string, string> snapshot)
	{
		foreach (var pair in snapshot)
		{
			if (!TryRestoreFile(pair.Key, pair.Value))
			{
				_console.WriteError(
					$"Cleanup: unable to restore '{pair.Key}'; " +
					"revert it manually.");
			}
		}
	}

	private bool TryRestoreFile(string path, string content)
	{
		const int MaxAttempts = 3;
		for (var attempt = 0; attempt < MaxAttempts; attempt++)
		{
			try
			{
				_fileSystem.WriteFile(path, content, overwrite: true);
				return true;
			}
			catch (IOException)
			{
				Thread.Sleep(250);
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		return false;
	}

	private async Task CleanupFailedModuleAsync(
		ModuleAddRequest request,
		ModulePipelineContext context)
	{
		RestoreFiles(context.ModifiedFiles);
		var apiProjectFile =
			$"{request.ProjectName}.Api/{request.ProjectName}.Api.csproj";
		await TryRunCleanupAsync(
			$"sln {request.ProjectName}.sln remove " +
			$"{context.ModuleProject}/{context.ModuleProject}.csproj",
			context.RepoRoot);
		await TryRunCleanupAsync(
			$"remove {apiProjectFile} reference " +
			$"{context.ModuleProject}/{context.ModuleProject}.csproj",
			context.RepoRoot);

		try
		{
			_fileSystem.DeleteDirectory(context.ModuleFolder);
		}
		catch (Exception ex)
		{
			_console.WriteError(
				$"Cleanup: unable to remove '{context.ModuleFolder}': " +
				ex.Message);
		}
	}

	private async Task TryRunCleanupAsync(
		string arguments,
		string repoRoot)
	{
		try
		{
			await _dotnet.RunAsync(arguments, repoRoot);
		}
		catch (Exception ex)
		{
			_console.WriteError(
				$"Cleanup: 'dotnet {arguments}' failed: {ex.Message}");
		}
	}
}

namespace ModMon.Cli;

internal sealed class ValidateCommandHandler
{
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;

	public ValidateCommandHandler(
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
			var errorResult = new ValidateResult(
				Success: false,
				Message: "Validation failed",
				ExitCode: ExitCode.ResourceNotFound,
				Timestamp: DateTime.UtcNow,
				Valid: false,
				Errors: new List<string>
				{
					"No solution file found in directory tree"
				});
			_output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.ResourceNotFound;
		}

		var checks = new List<ValidationCheck>();
		var errors = new List<string>();

		checks.Add(CheckSolutionExists(projectInfo, errors));
		checks.Add(CheckApiProjectExists(projectInfo, errors));
		checks.Add(CheckSharedKernelExists(projectInfo, errors));
		checks.Add(CheckDiAggregatorExists(projectInfo, errors));
		checks.Add(CheckDockerfileExists(projectInfo, errors));

		var allPassed = checks.All(c => c.Passed);

		var result = new ValidateResult(
			Success: true,
			Message: allPassed
				? "Project structure is valid"
				: "Project structure has issues",
			ExitCode: allPassed ? ExitCode.Success : ExitCode.ValidationFailed,
			Timestamp: DateTime.UtcNow,
			Valid: allPassed,
			ProjectName: projectInfo.ProjectName,
			Checks: checks,
			Errors: errors.Count > 0 ? errors : null);

		if (allPassed)
		{
			_output.WriteSuccess(result);
		}
		else
		{
			_output.WriteError(result.Message, result);
		}

		return (int)result.ExitCode;
	}

	private ValidationCheck CheckSolutionExists(
		ProjectInfo projectInfo,
		List<string> errors)
	{
		var exists = _fileSystem.FileExists(projectInfo.SolutionPath);
		if (!exists)
		{
			errors.Add($"Solution file not found: {projectInfo.SolutionPath}");
		}
		return new ValidationCheck(
			"Solution file exists",
			exists,
			exists ? null : $"Missing: {projectInfo.SolutionPath}");
	}

	private ValidationCheck CheckApiProjectExists(
		ProjectInfo projectInfo,
		List<string> errors)
	{
		var apiProject = $"{projectInfo.ProjectName}.Api";
		var exists = projectInfo.Projects.Contains(apiProject);
		if (!exists)
		{
			errors.Add($"API project not found: {apiProject}");
		}
		return new ValidationCheck(
			"API project exists",
			exists,
			exists ? null : $"Missing: {apiProject}");
	}

	private ValidationCheck CheckSharedKernelExists(
		ProjectInfo projectInfo,
		List<string> errors)
	{
		var kernelProject = $"{projectInfo.ProjectName}.SharedKernel";
		var exists = projectInfo.Projects.Contains(kernelProject);
		if (!exists)
		{
			errors.Add($"SharedKernel project not found: {kernelProject}");
		}
		return new ValidationCheck(
			"SharedKernel project exists",
			exists,
			exists ? null : $"Missing: {kernelProject}");
	}

	private ValidationCheck CheckDiAggregatorExists(
		ProjectInfo projectInfo,
		List<string> errors)
	{
		var diPath = Path.Combine(
			projectInfo.RepoRoot,
			$"{projectInfo.ProjectName}.Api",
			"Extensions",
			"DependencyInjection.cs");
		var exists = _fileSystem.FileExists(diPath);
		if (!exists)
		{
			errors.Add($"DI aggregator file not found: {diPath}");
		}
		return new ValidationCheck(
			"DI aggregator file exists",
			exists,
			exists ? null : $"Missing: {diPath}");
	}

	private ValidationCheck CheckDockerfileExists(
		ProjectInfo projectInfo,
		List<string> errors)
	{
		var dockerfilePath = Path.Combine(projectInfo.RepoRoot, "Dockerfile");
		var exists = _fileSystem.FileExists(dockerfilePath);
		if (!exists)
		{
			errors.Add($"Dockerfile not found: {dockerfilePath}");
		}
		return new ValidationCheck(
			"Dockerfile exists",
			exists,
			exists ? null : $"Missing: {dockerfilePath}");
	}
}

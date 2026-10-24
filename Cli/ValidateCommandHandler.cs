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

	public async Task<int> HandleAsync(string? path)
	{
		var startDirectory = string.IsNullOrWhiteSpace(path)
			? Directory.GetCurrentDirectory()
			: path;
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(startDirectory);

		return catalog.Error == null
			? await WriteValidationAsync(catalog)
			: WriteCatalogError(catalog);
	}

	private int WriteCatalogError(SolutionProjectCatalog catalog)
	{
		var exitCode = catalog.NotFound
			? ExitCode.ResourceNotFound
			: ExitCode.ValidationFailed;
		var check = new ValidationCheck(
			"Solution discovery",
			false,
			catalog.Error,
			ValidationRuleIds.SolutionDiscovery);
		var result = new ValidateResult(
			Success: false,
			Message: "Validation could not inspect the solution",
			ExitCode: exitCode,
			Timestamp: DateTime.UtcNow,
			Valid: false,
			Checks: new List<ValidationCheck> { check },
			Errors: new List<string> { catalog.Error! });

		_output.WriteSuccess(result);
		return (int)exitCode;
	}

	private async Task<int> WriteValidationAsync(
		SolutionProjectCatalog catalog)
	{
		var result = await CreateValidationResultAsync(catalog);
		_output.WriteSuccess(result);
		return (int)result.ExitCode;
	}

	private async Task<ValidateResult> CreateValidationResultAsync(
		SolutionProjectCatalog catalog)
	{
		var checks = await BuildChecksAsync(catalog);
		var allPassed = checks.All(check => check.Passed);
		var errors = checks
			.Where(check => !check.Passed)
			.Select(check => check.Message!)
			.ToList();
		return new ValidateResult(
			Success: true,
			Message: GetValidationMessage(allPassed),
			ExitCode: allPassed
				? ExitCode.Success
				: ExitCode.ValidationFailed,
			Timestamp: DateTime.UtcNow,
			Valid: allPassed,
			ProjectName: catalog.ProjectName,
			Checks: checks,
			Errors: errors.Count == 0 ? null : errors);
	}

	private static string GetValidationMessage(bool allPassed)
	{
		return allPassed
			? "Project structure and boundaries are valid"
			: "Project structure or boundaries have issues";
	}

	private async Task<List<ValidationCheck>> BuildChecksAsync(
		SolutionProjectCatalog catalog)
	{
		var checks = new List<ValidationCheck>
		{
			CheckSolutionExists(catalog),
			CheckApiProjectExists(catalog),
			CheckSharedKernelExists(catalog),
			CheckDiAggregatorExists(catalog),
			CheckDockerfileExists(catalog)
		};
		var boundaryValidator = new ProjectReferenceBoundaryValidator(
			new ProjectReferenceReader());
		checks.AddRange(await boundaryValidator.ValidateAsync(catalog));
		return checks;
	}

	private ValidationCheck CheckSolutionExists(
		SolutionProjectCatalog catalog)
	{
		var exists = _fileSystem.FileExists(catalog.SolutionPath!);
		return new ValidationCheck(
			"Solution file exists",
			exists,
			exists ? null : $"Missing: {catalog.SolutionPath}",
			ValidationRuleIds.SolutionExists);
	}

	private static ValidationCheck CheckApiProjectExists(
		SolutionProjectCatalog catalog)
	{
		var name = $"{catalog.ProjectName}.Api";
		var exists = catalog.Projects.Any(project => project.Name == name);
		return new ValidationCheck(
			"API project exists",
			exists,
			exists ? null : $"Missing: {name}",
			ValidationRuleIds.ApiProject);
	}

	private static ValidationCheck CheckSharedKernelExists(
		SolutionProjectCatalog catalog)
	{
		var name = $"{catalog.ProjectName}.SharedKernel";
		var exists = catalog.Projects.Any(project => project.Name == name);
		return new ValidationCheck(
			"SharedKernel project exists",
			exists,
			exists ? null : $"Missing: {name}",
			ValidationRuleIds.SharedKernelProject);
	}

	private ValidationCheck CheckDiAggregatorExists(
		SolutionProjectCatalog catalog)
	{
		var diPath = Path.Combine(
			catalog.RepoRoot!,
			$"{catalog.ProjectName}.Api",
			"Extensions",
			"DependencyInjection.cs");
		var exists = _fileSystem.FileExists(diPath);
		return new ValidationCheck(
			"DI aggregator file exists",
			exists,
			exists ? null : $"Missing: {diPath}",
			ValidationRuleIds.DiAggregator);
	}

	private ValidationCheck CheckDockerfileExists(
		SolutionProjectCatalog catalog)
	{
		var dockerfilePath = Path.Combine(catalog.RepoRoot!, "Dockerfile");
		var exists = _fileSystem.FileExists(dockerfilePath);
		return new ValidationCheck(
			"Dockerfile exists",
			exists,
			exists ? null : $"Missing: {dockerfilePath}",
			ValidationRuleIds.Dockerfile);
	}
}

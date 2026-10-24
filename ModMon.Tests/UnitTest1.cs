using ModMon.Cli;

namespace ModMon.Tests;

public sealed class ProjectReferenceBoundaryTests
{
	[Fact]
	public async Task AllowsApiModulesAndSharedKernelDependencies()
	{
		using var fixture = await CreateValidFixtureAsync();
		var checks = await ValidateGraphAsync(fixture);

		Assert.All(checks, check => Assert.True(check.Passed, check.Message));
	}

	[Fact]
	public async Task RejectsModuleToModuleDependency()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.Inventory");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectBoundary &&
			!check.Passed);
	}

	[Fact]
	public async Task RejectsSharedKernelDependencyOnModule()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.SetReferencesAsync(
			"Demo.SharedKernel",
			"Demo.Orders");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectBoundary &&
			!check.Passed);
	}

	[Fact]
	public async Task RejectsSharedKernelDependencyOnApi()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.SetReferencesAsync("Demo.SharedKernel", "Demo.Api");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectBoundary &&
			!check.Passed);
	}

	[Fact]
	public async Task RejectsReferenceOutsideSelectedSolution()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectAsync(
			"Demo.Api",
			"..\\Unlisted\\Unlisted.csproj");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectReference &&
			check.Message!.Contains("not in the selected solution"));
	}

	[Fact]
	public async Task ResolvesForwardSlashReferencePaths()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectAsync(
			"Demo.Api",
			"../Demo.Orders/Demo.Orders.csproj");
		var checks = await ValidateGraphAsync(fixture);

		Assert.All(checks, check => Assert.True(check.Passed, check.Message));
	}

	[Fact]
	public async Task ReportsMissingProjectFile()
	{
		using var fixture = await CreateValidFixtureAsync();
		File.Delete(fixture.ProjectPath("Demo.Inventory"));
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectFile &&
			check.Message!.Contains("Project file not found"));
	}

	[Fact]
	public async Task IgnoresPackageReferencesWhenBuildingProjectGraph()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectContentAsync(
			"Demo.Orders",
			"<Project><ItemGroup><PackageReference " +
			"Include=\"Demo.Inventory\" /></ItemGroup></Project>");
		var checks = await ValidateGraphAsync(fixture);

		Assert.All(checks, check => Assert.True(check.Passed, check.Message));
	}

	[Fact]
	public async Task ReportsDuplicateReferencesAndCycles()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectAsync(
			"Demo.Api",
			"..\\Demo.Orders\\Demo.Orders.csproj",
			"..\\Demo.Orders\\Demo.Orders.csproj");
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.Api");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectReference);
		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectCycle);
	}

	[Fact]
	public async Task FailsClosedOnUnresolvedReferenceExpression()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectAsync(
			"Demo.Api",
			"$(UnresolvedProject)/Target.csproj");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectFile &&
			check.Message!.Contains("Unresolved ProjectReference"));
	}

	[Fact]
	public async Task ReadsLiteralImportsForProjectReferences()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectContentAsync(
			"Demo.Orders",
			"<Project Sdk=\"Microsoft.NET.Sdk\">" +
			"<Import Project=\"References.props\" /></Project>");
		var propsPath = Path.Combine(
			Path.GetDirectoryName(fixture.ProjectPath("Demo.Orders"))!,
			"References.props");
		await File.WriteAllTextAsync(
			propsPath,
			"<Project><ItemGroup><ProjectReference " +
			"Include=\"..\\Demo.SharedKernel\\Demo.SharedKernel.csproj\" />" +
			"</ItemGroup></Project>");
		var checks = await ValidateGraphAsync(fixture);

		Assert.All(checks, check => Assert.True(check.Passed, check.Message));
	}

	[Fact]
	public async Task FailsClosedOnUnresolvedImport()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectContentAsync(
			"Demo.Orders",
			"<Project><Import Project=\"$(UnknownTargets)\" /></Project>");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectFile &&
			check.Message!.Contains("Unresolved project import"));
	}

	[Fact]
	public async Task ReportsMalformedProjectXml()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectContentAsync("Demo.Orders", "<Project>");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectFile &&
			check.Message!.Contains("Unable to parse"));
	}

	[Fact]
	public async Task ChecksLiteralReferencesInEveryCondition()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.WriteProjectContentAsync(
			"Demo.Orders",
			"<Project><ItemGroup><ProjectReference " +
			"Include=\"..\\Demo.Inventory\\Demo.Inventory.csproj\" " +
			"Condition=\"'$(Configuration)' == 'Never'\" />" +
			"</ItemGroup></Project>");
		var checks = await ValidateGraphAsync(fixture);

		Assert.Contains(checks, check =>
			check.RuleId == ValidationRuleIds.ProjectBoundary);
	}

	[Fact]
	public async Task IgnoresTestProjectAsBoundarySource()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.AddProjectAsync("Demo.Orders.Tests");
		await fixture.SetReferencesAsync(
			"Demo.Orders.Tests",
			"Demo.Orders");
		await fixture.WriteSolutionAsync();
		var checks = await ValidateGraphAsync(fixture);

		Assert.All(checks, check => Assert.True(check.Passed, check.Message));
	}

	[Fact]
	public async Task DiscoversSolutionFromNestedProjectPath()
	{
		using var fixture = await CreateValidFixtureAsync();
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.ProjectPath("Demo.Api"));

		Assert.Null(catalog.Error);
		Assert.Equal("Demo", catalog.ProjectName);
	}

	[Fact]
	public async Task RejectsSolutionProjectOutsideRoot()
	{
		using var fixture = await CreateValidFixtureAsync();
		var solution = "Project(\"{ABC}\") = \"External\", " +
			"\"..\\Outside\\Outside.csproj\", \"{DEF}\"";
		await File.WriteAllTextAsync(
			Path.Combine(fixture.Root, "Demo.sln"),
			solution);
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.Root);

		Assert.Contains("outside the solution root", catalog.Error);
	}

	[Fact]
	public async Task RejectsDuplicateSolutionProjectEntries()
	{
		using var fixture = await CreateValidFixtureAsync();
		var solutionPath = Path.Combine(fixture.Root, "Demo.sln");
		var lines = await File.ReadAllLinesAsync(solutionPath);
		await File.WriteAllLinesAsync(solutionPath, lines.Append(lines[1]));
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.Root);

		Assert.Contains("Duplicate project entry", catalog.Error);
	}

	[Fact]
	public async Task ReportsMissingSolution()
	{
		using var fixture = new ProjectFixture();
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.Root);

		Assert.True(catalog.NotFound);
		Assert.NotNull(catalog.Error);
	}

	[Fact]
	public async Task MissingSolutionReturnsResourceNotFound()
	{
		using var fixture = new ProjectFixture();
		var output = new RecordingOutputWriter();
		var handler = new ValidateCommandHandler(
			new PhysicalFileSystemWriter(),
			output);

		var exitCode = await handler.HandleAsync(fixture.Root);
		var result = Assert.IsType<ValidateResult>(output.Result);

		Assert.Equal((int)ExitCode.ResourceNotFound, exitCode);
		Assert.False(result.Valid);
		Assert.Contains("No solution file found", result.Errors![0]);
	}

	[Fact]
	public async Task ReportsAmbiguousSolutionInsteadOfSearchingParent()
	{
		using var fixture = await CreateValidFixtureAsync();
		await File.WriteAllTextAsync(
			Path.Combine(fixture.Root, "Second.sln"),
			"Microsoft Visual Studio Solution File");
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.Root);

		Assert.NotNull(catalog.Error);
		Assert.False(catalog.NotFound);
	}

	[Fact]
	public async Task JsonFailureOutputRetainsBoundaryDetails()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.Inventory");
		await CreateStructuralFilesAsync(fixture.Root);
		var json = await CaptureJsonAsync(fixture);

		Assert.Contains("\"valid\": false", json);
		Assert.Contains("\"exitCode\": \"ValidationFailed\"", json);
		Assert.Contains("\"ruleId\": \"project-boundary\"", json);
	}

	[Fact]
	public async Task ReturnsValidationReportWithBoundaryFailures()
	{
		using var fixture = await CreateValidFixtureAsync();
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.Inventory");
		await CreateStructuralFilesAsync(fixture.Root);
		var output = new RecordingOutputWriter();
		var handler = new ValidateCommandHandler(
			new PhysicalFileSystemWriter(),
			output);

		var exitCode = await handler.HandleAsync(fixture.Root);
		var result = Assert.IsType<ValidateResult>(output.Result);

		Assert.Equal((int)ExitCode.ValidationFailed, exitCode);
		Assert.False(result.Valid);
		Assert.Contains(result.Checks!, check => !check.Passed);
	}

	private static async Task<ProjectFixture> CreateValidFixtureAsync()
	{
		var fixture = new ProjectFixture();
		await fixture.AddProjectAsync("Demo.Api");
		await fixture.AddProjectAsync("Demo.SharedKernel");
		await fixture.AddProjectAsync("Demo.Orders");
		await fixture.AddProjectAsync("Demo.Inventory");
		await fixture.SetReferencesAsync(
			"Demo.Api",
			"Demo.Orders",
			"Demo.Inventory",
			"Demo.SharedKernel");
		await fixture.SetReferencesAsync(
			"Demo.Orders",
			"Demo.SharedKernel");
		await fixture.SetReferencesAsync(
			"Demo.Inventory",
			"Demo.SharedKernel");
		await fixture.WriteSolutionAsync();
		return fixture;
	}

	private static async Task<List<ValidationCheck>> ValidateGraphAsync(
		ProjectFixture fixture)
	{
		var catalog = await new SolutionProjectCatalogReader()
			.ReadAsync(fixture.Root);
		Assert.Null(catalog.Error);
		return await new ProjectReferenceBoundaryValidator(
			new ProjectReferenceReader()).ValidateAsync(catalog);
	}

	private static async Task CreateStructuralFilesAsync(string root)
	{
		var apiPath = Path.Combine(
			root,
			"Demo.Api",
			"Extensions",
			"DependencyInjection.cs");
		Directory.CreateDirectory(Path.GetDirectoryName(apiPath)!);
		await File.WriteAllTextAsync(apiPath, string.Empty);
		await File.WriteAllTextAsync(
			Path.Combine(root, "Dockerfile"),
			string.Empty);
	}

	private static async Task<string> CaptureJsonAsync(
		ProjectFixture fixture)
	{
		var previousOutput = Console.Out;
		using var capturedOutput = new StringWriter();
		try
		{
			Console.SetOut(capturedOutput);
			var output = new JsonOutputWriter();
			var handler = new ValidateCommandHandler(
				new PhysicalFileSystemWriter(),
				output);
			await handler.HandleAsync(fixture.Root);
			output.Flush();
		}
		finally
		{
			Console.SetOut(previousOutput);
		}

		return capturedOutput.ToString();
	}
}

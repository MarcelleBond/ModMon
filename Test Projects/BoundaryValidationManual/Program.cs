using System.Diagnostics;
using BoundaryValidationManual;

var repositoryRoot = FindRepositoryRoot();
var baselineRoot = CreateTemporaryRoot("baseline");
var scenarios = CreateScenarios();
var failures = 0;

try
{
	await CreateGeneratedSolutionAsync(baselineRoot, repositoryRoot);
	await VerifyGeneratedGuidanceAsync(baselineRoot);
	failures = await RunScenariosAsync(
		repositoryRoot,
		baselineRoot,
		scenarios);
}
finally
{
	DeleteDirectory(baselineRoot);
}

Console.WriteLine($"Manual scenarios: {scenarios.Count - failures} passed, " +
	$"{failures} failed.");
return failures == 0 ? 0 : 1;

static List<ManualScenario> CreateScenarios()
{
	return new List<ManualScenario>
	{
		new("valid", 0, CreateValidAsync),
		new("module-to-module", 5, CreateModuleViolationAsync),
		new("shared-kernel-to-module", 5, CreateKernelViolationAsync),
		new("external-reference", 5, CreateExternalReferenceAsync),
		new("dynamic-reference", 5, CreateDynamicReferenceAsync),
		new("dependency-cycle", 5, CreateCycleAsync),
		new("malformed-project", 5, CreateMalformedProjectAsync),
		new("missing-solution", 4, CreateMissingSolutionAsync)
	};
}

static async Task<int> RunScenariosAsync(
	string repositoryRoot,
	string baselineRoot,
	List<ManualScenario> scenarios)
{
	var failures = 0;
	foreach (var scenario in scenarios)
	{
		var result = await RunScenarioAsync(
			repositoryRoot,
			baselineRoot,
			scenario);
		Console.WriteLine(result.Output);
		if (result.ExitCode != scenario.ExpectedExitCode)
		{
			failures++;
			Console.Error.WriteLine(
				$"FAIL: {scenario.Name}; expected " +
				$"{scenario.ExpectedExitCode}, got {result.ExitCode}");
		}
	}

	return failures;
}

static async Task<ManualRunResult> RunScenarioAsync(
	string repositoryRoot,
	string baselineRoot,
	ManualScenario scenario)
{
	var scenarioRoot = CreateTemporaryRoot(scenario.Name);
	try
	{
		await scenario.CreateAsync(
			scenarioRoot,
			repositoryRoot,
			baselineRoot);
		return await RunValidateAsync(repositoryRoot, scenarioRoot);
	}
	finally
	{
		DeleteDirectory(scenarioRoot);
	}
}

static async Task<ManualRunResult> RunValidateAsync(
	string repositoryRoot,
	string scenarioRoot)
{
	return await RunCommandAsync(
		repositoryRoot,
		"validate",
		"--path",
		scenarioRoot,
		"--format",
		"json");
}

static async Task<ManualRunResult> RunCommandAsync(
	string repositoryRoot,
	params string[] arguments)
{
	var startInfo = CreateStartInfo(repositoryRoot, arguments);
	using var process = Process.Start(startInfo)!;
	var outputTask = process.StandardOutput.ReadToEndAsync();
	var errorTask = process.StandardError.ReadToEndAsync();
	await process.WaitForExitAsync();
	var output = await outputTask;
	var error = await errorTask;
	return new ManualRunResult(
		process.ExitCode,
		$"--- {string.Join(' ', arguments)} ---\n" + output + error);
}

static ProcessStartInfo CreateStartInfo(
	string repositoryRoot,
	IEnumerable<string> arguments)
{
	var startInfo = new ProcessStartInfo
	{
		FileName = "dotnet",
		WorkingDirectory = repositoryRoot,
		RedirectStandardOutput = true,
		RedirectStandardError = true,
		UseShellExecute = false
	};
	startInfo.ArgumentList.Add("run");
	startInfo.ArgumentList.Add("--project");
	startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "modmon.csproj"));
	startInfo.ArgumentList.Add("--no-build");
	startInfo.ArgumentList.Add("--");
	foreach (var argument in arguments)
	{
		startInfo.ArgumentList.Add(argument);
	}

	return startInfo;
}

static async Task CreateGeneratedSolutionAsync(
	string root,
	string repositoryRoot)
{
	await RunSuccessfulCommandAsync(
		repositoryRoot,
		"init",
		"--name",
		"Demo",
		"--output",
		root);
	await AddModuleAsync(root, repositoryRoot, "Orders");
	await AddModuleAsync(root, repositoryRoot, "Products");
}

static async Task VerifyGeneratedGuidanceAsync(string root)
{
	var files = new[]
	{
		Path.Combine(root, "AGENTS.md"),
		Path.Combine(root, ".agent", "modmon", "SKILL.md"),
		Path.Combine(root, ".agent", "modmon", "architecture-patterns.md"),
		Path.Combine(root, ".agent", "modmon", "quick-reference.md")
	};
	foreach (var file in files)
	{
		if (!File.Exists(file))
		{
			throw new InvalidOperationException(
				$"Expected generated guidance file was missing: {file}");
		}

		var content = await File.ReadAllTextAsync(file);
		if (!content.Contains("Demo", StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"Generated guidance did not contain the project name: {file}");
		}
	}
}

static async Task AddModuleAsync(
	string root,
	string repositoryRoot,
	string moduleName)
{
	await RunSuccessfulCommandAsync(
		repositoryRoot,
		"module",
		"add",
		"--name",
		moduleName,
		"--output",
		root);
}

static async Task RunSuccessfulCommandAsync(
	string repositoryRoot,
	params string[] arguments)
{
	var result = await RunCommandAsync(repositoryRoot, arguments);
	if (result.ExitCode != 0)
	{
		throw new InvalidOperationException(
			$"Setup command failed with exit {result.ExitCode}: " +
			result.Output);
	}
}

static async Task CreateValidAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
}

static async Task CreateModuleViolationAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await SetReferencesAsync(root, "Demo.Orders", "Demo.Products");
}

static async Task CreateKernelViolationAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await SetReferencesAsync(root, "Demo.SharedKernel", "Demo.Orders");
}

static async Task CreateExternalReferenceAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await WriteProjectAsync(root, "Demo.Api", "../Outside/Outside.csproj");
}

static async Task CreateDynamicReferenceAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await WriteProjectAsync(root, "Demo.Api", "$(UnknownProject)/Target.csproj");
}

static async Task CreateCycleAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await SetReferencesAsync(root, "Demo.Orders", "Demo.Api");
}

static async Task CreateMalformedProjectAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	await CopyBaselineAsync(root, baselineRoot);
	await File.WriteAllTextAsync(
		ProjectPath(root, "Demo.Orders"),
		"<Project>");
}

static Task CreateMissingSolutionAsync(
	string root,
	string repositoryRoot,
	string baselineRoot)
{
	return Task.CompletedTask;
}

static async Task WriteProjectAsync(
	string root,
	string name,
	params string[] references)
{
	var items = references.Length == 0
		? string.Empty
		: $"<ItemGroup>{string.Join(string.Empty, references.Select(reference =>
			$"<ProjectReference Include=\"{reference}\" />"))}</ItemGroup>";
	var content = $"<Project Sdk=\"Microsoft.NET.Sdk\">{items}</Project>";
	await File.WriteAllTextAsync(ProjectPath(root, name), content);
}

static async Task SetReferencesAsync(
	string root,
	string source,
	params string[] targets)
{
	var sourceDirectory = Path.GetDirectoryName(ProjectPath(root, source))!;
	var references = targets.Select(target => Path.GetRelativePath(
		sourceDirectory,
		ProjectPath(root, target)).Replace(Path.DirectorySeparatorChar, '\\'));
	await WriteProjectAsync(root, source, references.ToArray());
}

static async Task CopyBaselineAsync(string destination, string source)
{
	Directory.CreateDirectory(destination);
	foreach (var file in GetValidationFiles(source))
	{
		var relative = Path.GetRelativePath(source, file);
		var target = Path.Combine(destination, relative);
		Directory.CreateDirectory(Path.GetDirectoryName(target)!);
		var content = await File.ReadAllBytesAsync(file);
		await File.WriteAllBytesAsync(target, content);
	}
}

static IEnumerable<string> GetValidationFiles(string root)
{
	return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
		.Where(path => Path.GetFileName(path).Equals(
			"Demo.sln",
			StringComparison.OrdinalIgnoreCase) ||
			Path.GetFileName(path).Equals(
			"Dockerfile",
			StringComparison.OrdinalIgnoreCase) ||
			Path.GetExtension(path).Equals(
			".csproj",
			StringComparison.OrdinalIgnoreCase) ||
			path.Contains(
				$"Demo.Api{Path.DirectorySeparatorChar}Extensions"));
}

static string CreateTemporaryRoot(string name)
{
	var root = Path.Combine(
		Path.GetTempPath(),
		$"modmon-manual-{name}-{Guid.NewGuid():N}");
	Directory.CreateDirectory(root);
	return root;
}

static void DeleteDirectory(string path)
{
	if (Directory.Exists(path))
	{
		Directory.Delete(path, recursive: true);
	}
}

static string ProjectPath(string root, string name)
{
	return Path.Combine(root, name, $"{name}.csproj");
}

static string FindRepositoryRoot()
{
	var directory = new DirectoryInfo(AppContext.BaseDirectory);
	while (directory != null)
	{
		if (File.Exists(Path.Combine(directory.FullName, "modmon.csproj")))
		{
			return directory.FullName;
		}

		directory = directory.Parent;
	}

	throw new InvalidOperationException("Repository root was not found.");
}

internal sealed record ManualRunResult(int ExitCode, string Output);

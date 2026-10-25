namespace ModMon.Cli;

internal sealed class ProjectInfoProvider
{
	private readonly IFileSystemWriter _fileSystem;

	public ProjectInfoProvider(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public ProjectInfoLookup GetProjectInfo(string startDirectory)
	{
		var lookup = new SolutionLocator().GetSolutionInfo(startDirectory);
		if (lookup.Info == null)
		{
			return new ProjectInfoLookup(null, lookup.Error);
		}

		var modules = ScanForModules(
			lookup.Info.RepoRoot,
			lookup.Info.ProjectName);
		var projects = ScanForProjects(lookup.Info.RepoRoot);

		return new ProjectInfoLookup(
			new ProjectInfo(
				lookup.Info.ProjectName,
				Path.Combine(
					lookup.Info.RepoRoot,
					$"{lookup.Info.ProjectName}.sln"),
				lookup.Info.RepoRoot,
				modules,
				projects),
			null);
	}

	private List<ModuleInfo> ScanForModules(
		string repoRoot,
		string projectName)
	{
		var modules = new List<ModuleInfo>();
		var directories = Directory.GetDirectories(repoRoot);

		foreach (var dir in directories)
		{
			var dirName = Path.GetFileName(dir);
			if (!dirName.StartsWith($"{projectName}.", StringComparison.Ordinal))
			{
				continue;
			}

			if (dirName == $"{projectName}.Api" ||
				dirName == $"{projectName}.SharedKernel" ||
				dirName.EndsWith(".Tests", StringComparison.Ordinal))
			{
				continue;
			}

			if (!_fileSystem.FileExists(
				Path.Combine(dir, $"{dirName}.csproj")))
			{
				continue;
			}

			var moduleName = dirName.Substring(projectName.Length + 1);
			var hasDbContext = _fileSystem.FileExists(
				Path.Combine(dir, "Database", $"{moduleName}DbContext.cs"));
			var hasMigrations = _fileSystem.DirectoryExists(
				Path.Combine(dir, "Database", "Migrations"));

			modules.Add(new ModuleInfo(
				moduleName,
				dir,
				hasDbContext,
				hasMigrations));
		}

		return modules
			.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<string> ScanForProjects(string repoRoot)
	{
		var projects = new List<string>();
		var csprojFiles = Directory.GetFiles(
			repoRoot,
			"*.csproj",
			SearchOption.AllDirectories);

		foreach (var csproj in csprojFiles)
		{
			if (csproj.Contains(
				$"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
				StringComparison.OrdinalIgnoreCase) ||
				csproj.Contains(
				$"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
				StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var projectName = Path.GetFileNameWithoutExtension(csproj);
			projects.Add(projectName);
		}

		return projects
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}

internal record ProjectInfo(
	string ProjectName,
	string SolutionPath,
	string RepoRoot,
	List<ModuleInfo> Modules,
	List<string> Projects);

internal record ProjectInfoLookup(
	ProjectInfo? Info,
	string? Error);

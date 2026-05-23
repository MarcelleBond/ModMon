namespace ModMon.Cli;

internal sealed class ProjectInfoProvider
{
	private readonly IFileSystemWriter _fileSystem;

	public ProjectInfoProvider(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public ProjectInfo? GetProjectInfo(string startDirectory)
	{
		var solutionLocator = new SolutionLocator();
		var solutionInfo = solutionLocator.TryGetSolutionInfo(startDirectory);

		if (solutionInfo == null)
		{
			return null;
		}

		var modules = ScanForModules(
			solutionInfo.RepoRoot,
			solutionInfo.ProjectName);
		var projects = ScanForProjects(solutionInfo.RepoRoot);

		return new ProjectInfo(
			solutionInfo.ProjectName,
			Path.Combine(solutionInfo.RepoRoot, $"{solutionInfo.ProjectName}.sln"),
			solutionInfo.RepoRoot,
			modules,
			projects);
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
				dirName == $"{projectName}.SharedKernel")
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

		return modules;
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
			var projectName = Path.GetFileNameWithoutExtension(csproj);
			projects.Add(projectName);
		}

		return projects;
	}
}

internal record ProjectInfo(
	string ProjectName,
	string SolutionPath,
	string RepoRoot,
	List<ModuleInfo> Modules,
	List<string> Projects);

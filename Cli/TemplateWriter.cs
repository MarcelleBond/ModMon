namespace ModMon.Cli;

internal sealed class TemplateWriter
{
	private const bool Overwrite = true;
	private readonly IFileSystemWriter _fileSystem;

	public TemplateWriter(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public void WriteApiBaseline(string repoRoot, string projectName)
	{
		var apiFolder = Path.Combine(repoRoot, $"{projectName}.Api");
		_fileSystem.WriteFile(
			Path.Combine(apiFolder, "Program.cs"),
			Templates.ApiProgram(projectName),
			overwrite: Overwrite);

		_fileSystem.WriteFile(
			Path.Combine(apiFolder, "appsettings.Development.json"),
			Templates.ApiAppSettingsDevelopment(projectName),
			overwrite: Overwrite);

		SerilogApiExtensionsTemplates.WriteAll(
			_fileSystem,
			apiFolder,
			projectName);
	}

	public void WriteRootDockerFiles(string repoRoot, string projectName)
	{
		_fileSystem.WriteFile(
			Path.Combine(repoRoot, "Dockerfile"),
			Templates.RootDockerfile(projectName),
			overwrite: Overwrite);

		_fileSystem.WriteFile(
			Path.Combine(repoRoot, "docker-compose.yml"),
			Templates.DockerCompose(projectName),
			overwrite: Overwrite);
	}

	public void UpdateRootDockerFiles(
		string repoRoot,
		string projectName,
		string moduleName)
	{
		var dockerfile = Path.Combine(repoRoot, "Dockerfile");
		var updater = new DockerfileModulesUpdater(_fileSystem);
		updater.AddModuleProject(dockerfile, projectName, moduleName);
	}

	public void WriteSharedKernelTemplates(string repoRoot, string projectName)
	{
		var kernel = Path.Combine(repoRoot, $"{projectName}.SharedKernel");
		SharedKernelTemplates.WriteAll(_fileSystem, kernel, projectName);
	}

	public void WriteApiDiAggregator(string repoRoot, string projectName)
	{
		var apiExtensions = Path.Combine(
			repoRoot,
			$"{projectName}.Api",
			"Extensions");

		_fileSystem.WriteFile(
			Path.Combine(apiExtensions, "DependencyInjection.cs"),
			Templates.ApiModulesAggregator(projectName, Array.Empty<string>()),
			overwrite: Overwrite);
	}

	public void WriteModuleTemplates(
		string repoRoot,
		string projectName,
		string moduleName)
	{
		var moduleProject = $"{projectName}.{moduleName}";
		var moduleRoot = Path.Combine(repoRoot, moduleProject);
		ModuleTemplates.WriteAll(_fileSystem, moduleRoot, projectName, moduleName);
	}

	public void UpdateApiDiAggregator(
		string repoRoot,
		string projectName,
		string moduleName)
	{
		var path = Path.Combine(
			repoRoot,
			$"{projectName}.Api",
			"Extensions",
			"DependencyInjection.cs");

		var updater = new ApiDiAggregatorUpdater(_fileSystem);
		updater.AddModule(path, projectName, moduleName);
	}
}

using ModMon.Cli;

namespace ModMon.Tests;

public sealed class ScaffolderTests
{
	private readonly FakeFileSystemWriter _fs = new();
	private readonly RecordingConsole _console = new();
	private readonly string _root = @"C:\repo";

	[Fact]
	public async Task InitSucceedsAndReportsProjects()
	{
		var runner = new RecordingRunner();
		var scaffolder = new InitScaffolder(_console, runner, _fs);

		var result = await scaffolder.ScaffoldAsync(
			_root, "Demo", new DryRunTracker(false));

		Assert.True(result.Success);
		Assert.Equal(
			new[] { "Demo.Api", "Demo.SharedKernel" },
			result.CreatedProjects);
		Assert.Contains(
			result.CreatedFiles!,
			f => f.EndsWith("docker-compose.yml"));
		Assert.Contains(
			result.CreatedFiles!, f => f.EndsWith(".dockerignore"));
	}

	[Fact]
	public async Task InitFailurePreservesPreExistingArtifacts()
	{
		var runner = new RecordingRunner(failOnCommand: 2, exitCode: 5);
		var scaffolder = new InitScaffolder(_console, runner, _fs);
		var preExisting = Path.Combine(_root, "Demo.Api");
		_fs.EnsureDirectory(preExisting);

		var result = await scaffolder.ScaffoldAsync(
			_root, "Demo", new DryRunTracker(false));

		Assert.False(result.Success);
		Assert.Equal(ExitCode.ExternalToolError, result.ExitCode);
		Assert.True(_fs.DirectoryExists(preExisting));
	}

	[Fact]
	public async Task ModuleAddRejectsExistingModuleDir()
	{
		var runner = new RecordingRunner();
		var scaffolder = new ModuleScaffolder(_console, runner, _fs);
		_fs.EnsureDirectory(Path.Combine(_root, "Demo.Orders"));

		var result = await scaffolder.ScaffoldAsync(
			_root,
			new ModuleAddRequest(_root, "Demo", "Orders"),
			new DryRunTracker(false));

		Assert.False(result.Success);
		Assert.Equal(ExitCode.ResourceAlreadyExists, result.ExitCode);
		Assert.Empty(runner.Commands);
	}

	[Fact]
	public async Task ModuleAddSucceedsWithMigrationAndFiles()
	{
		var runner = new RecordingRunner();
		SeedApiIntegrationFiles();
		var scaffolder = new ModuleScaffolder(_console, runner, _fs);

		var result = await scaffolder.ScaffoldAsync(
			_root,
			new ModuleAddRequest(_root, "Demo", "Orders"),
			new DryRunTracker(false));

		Assert.True(result.Success);
		Assert.True(result.MigrationCreated);
		Assert.Null(result.Warnings);
		Assert.NotEmpty(result.CreatedFiles!);
		Assert.Contains(
			"AddOrdersDI",
			_fs.ReadFile(Path.Combine(
				_root, "Demo.Api", "Extensions",
				"DependencyInjection.cs")));
	}

	[Fact]
	public async Task ModuleAddWarnsWhenIntegrationFilesMissing()
	{
		var runner = new RecordingRunner();
		var scaffolder = new ModuleScaffolder(_console, runner, _fs);

		var result = await scaffolder.ScaffoldAsync(
			_root,
			new ModuleAddRequest(_root, "Demo", "Orders"),
			new DryRunTracker(false));

		Assert.True(result.Success);
		Assert.Contains(result.Warnings!,
			w => w.Contains("DI aggregator"));
		Assert.Contains(result.Warnings!,
			w => w.Contains("Dockerfile"));
	}

	[Fact]
	public async Task ModuleFailureRestoresSnapshotAndCleansUp()
	{
		var runner = new RecordingRunner(failOnCommand: 5, exitCode: 3);
		var diPath = Path.Combine(
			_root, "Demo.Api", "Extensions", "DependencyInjection.cs");
		var dockerPath = Path.Combine(_root, "Dockerfile");
		_fs.WriteFile(diPath, "original-di", overwrite: false);
		_fs.WriteFile(dockerPath, "original-docker", overwrite: false);
		_fs.EnsureDirectory(Path.Combine(_root, "Demo.Orders"));
		_fs.DeleteDirectory(Path.Combine(_root, "Demo.Orders"));

		var scaffolder = new ModuleScaffolder(_console, runner, _fs);
		var result = await scaffolder.ScaffoldAsync(
			_root,
			new ModuleAddRequest(_root, "Demo", "Orders"),
			new DryRunTracker(false));

		Assert.False(result.Success);
		Assert.Equal(ExitCode.ExternalToolError, result.ExitCode);
		Assert.Contains(runner.Commands,
			c => c.StartsWith("sln Demo.sln remove"));
		Assert.Contains(runner.Commands,
			c => c.StartsWith("remove Demo.Api/Demo.Api.csproj"));
		Assert.False(_fs.DirectoryExists(
			Path.Combine(_root, "Demo.Orders")));
		Assert.Equal("original-di", _fs.ReadFile(diPath));
		Assert.Equal("original-docker", _fs.ReadFile(dockerPath));
	}

	[Fact]
	public async Task ModuleTemplateExceptionTriggersRollback()
	{
		var runner = new RecordingRunner();
		var dockerPath = Path.Combine(_root, "Dockerfile");
		_fs.WriteFile(
			dockerPath,
			"FROM sdk\n# <modules>\n# </modules>\n",
			overwrite: false);
		var scaffolder = new ModuleScaffolder(
			_console, runner, new ThrowingFileSystemWriter(_fs, dockerPath));

		var result = await scaffolder.ScaffoldAsync(
			_root,
			new ModuleAddRequest(_root, "Demo", "Orders"),
			new DryRunTracker(false));

		Assert.False(result.Success);
		Assert.Equal(ExitCode.GeneralFailure, result.ExitCode);
		Assert.Contains(runner.Commands,
			c => c.StartsWith("sln Demo.sln remove"));
	}

	private void SeedApiIntegrationFiles()
	{
		var diPath = Path.Combine(
			_root, "Demo.Api", "Extensions", "DependencyInjection.cs");
		_fs.WriteFile(diPath, """
using Demo.SharedKernel;

public static class DependencyInjection
{
	public static IServiceCollection AddProjectModules(
		this IServiceCollection services, IConfiguration c)
	{
		services.AddSharedKernelDI(c);
		// <modules>
		// </modules>
		return services;
	}

	public static WebApplication AddProjectDbUpMigrations(
		this WebApplication app, IConfiguration c)
	{
		// <dbup>
		// </dbup>
		return app;
	}
}
""", overwrite: false);
		_fs.WriteFile(Path.Combine(_root, "Dockerfile"), """
FROM sdk
# <modules>
# </modules>
RUN restore
""", overwrite: false);
	}

	private sealed class RecordingRunner : IDotnetCliRunner
	{
		private readonly int _failOn;
		private readonly int _exitCode;

		public List<string> Commands { get; } = new();

		public RecordingRunner(int failOnCommand = -1, int exitCode = 1)
		{
			_failOn = failOnCommand;
			_exitCode = exitCode;
		}

		public Task<int> RunAsync(
			string arguments,
			string workingDirectory)
		{
			Commands.Add(arguments);
			var failed = _failOn > 0 && Commands.Count == _failOn;
			return Task.FromResult(failed ? _exitCode : 0);
		}
	}

	private sealed class ThrowingFileSystemWriter : IFileSystemWriter
	{
		private readonly IFileSystemWriter _inner;
		private readonly string _throwOn;

		public ThrowingFileSystemWriter(
			IFileSystemWriter inner,
			string throwOn)
		{
			_inner = inner;
			_throwOn = throwOn;
		}

		public void EnsureDirectory(string path)
			=> _inner.EnsureDirectory(path);

		public void WriteFile(
			string path, string content, bool overwrite)
		{
			if (path == _throwOn)
			{
				throw new IOException("simulated lock");
			}

			_inner.WriteFile(path, content, overwrite);
		}

		public void DeleteFile(string path) => _inner.DeleteFile(path);
		public void DeleteDirectory(string path)
			=> _inner.DeleteDirectory(path);
		public bool FileExists(string path) => _inner.FileExists(path);
		public string ReadFile(string path) => _inner.ReadFile(path);
		public bool DirectoryExists(string path)
			=> _inner.DirectoryExists(path);
	}
}

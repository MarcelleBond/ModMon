using ModMon.Cli;

namespace ModMon.Tests;

public sealed class StepTests
{
	private readonly FakeFileSystemWriter _fs = new();
	private readonly RecordingConsole _console = new();
	private readonly string _root = @"C:\repo";

	[Fact]
	public async Task InitStepsRunAllCommandsInOrder()
	{
		var runner = new RecordingRunner();
		var steps = new DotnetInitSteps(runner, _console, _fs);

		var exit = await steps.RunAsync(
			_root, "Demo", "Demo.Api", "Demo.SharedKernel");

		Assert.Equal(0, exit);
		Assert.Equal(10, runner.Commands.Count);
		Assert.Equal("new sln -n Demo", runner.Commands[0]);
		Assert.Equal("new webapi -n Demo.Api", runner.Commands[1]);
		Assert.Contains(
			runner.Commands,
			c => c == "add Demo.Api/Demo.Api.csproj " +
				"reference Demo.SharedKernel/Demo.SharedKernel.csproj");
	}

	[Fact]
	public async Task InitStepsStopOnFirstFailure()
	{
		var runner = new RecordingRunner(failOnCommand: 3, exitCode: 7);
		var steps = new DotnetInitSteps(runner, _console, _fs);

		var exit = await steps.RunAsync(
			_root, "Demo", "Demo.Api", "Demo.SharedKernel");

		Assert.Equal(7, exit);
		Assert.Equal(3, runner.Commands.Count);
	}

	[Fact]
	public async Task InitStepsDeleteClass1WhenPresent()
	{
		var runner = new RecordingRunner();
		var stale = Path.Combine(
			_root, "Demo.SharedKernel", "Class1.cs");
		_fs.WriteFile(stale, "// stale", overwrite: false);
		var steps = new DotnetInitSteps(runner, _console, _fs);

		await steps.RunAsync(_root, "Demo", "Demo.Api", "Demo.SharedKernel");

		Assert.False(_fs.FileExists(stale));
	}

	[Fact]
	public async Task ModuleStepsEmbedSqlScriptsInCsproj()
	{
		var runner = new RecordingRunner();
		var csproj = Path.Combine(
			_root, "Demo.Orders", "Demo.Orders.csproj");
		_fs.WriteFile(
			csproj,
			"<Project Sdk=\"x\">\n</Project>\n",
			overwrite: false);
		var steps = new DotnetModuleSteps(runner, _console, _fs);

		var exit = await steps.RunAsync(
			_root, "Demo", "Demo.Api", "Demo.SharedKernel", "Demo.Orders");

		Assert.Equal(0, exit);
		var updated = _fs.ReadFile(csproj);
		Assert.Contains(
			"<EmbeddedResource Include=\"Database\\Scripts\\**\\*.sql\"",
			updated);
		Assert.Contains(
			"<FrameworkReference Include=\"Microsoft.AspNetCore.App\"",
			updated);
		Assert.Equal(10, runner.Commands.Count);
		Assert.Equal("new classlib -n Demo.Orders", runner.Commands[0]);
	}

	[Fact]
	public async Task ModuleStepsDoNotDuplicateSqlEmbedding()
	{
		var runner = new RecordingRunner();
		var csproj = Path.Combine(
			_root, "Demo.Orders", "Demo.Orders.csproj");
		_fs.WriteFile(
			csproj,
			"<Project>\n" +
			"  <EmbeddedResource Include=\"Database\\Scripts\\**\\*.sql\" />\n" +
			"</Project>\n",
			overwrite: false);
		var steps = new DotnetModuleSteps(runner, _console, _fs);

		await steps.RunAsync(
			_root, "Demo", "Demo.Api", "Demo.SharedKernel", "Demo.Orders");

		Assert.Equal(1, _fs.ReadFile(csproj)
			.Split("EmbeddedResource").Length - 1);
	}

	[Fact]
	public async Task ModuleStepsDoNotDuplicateFrameworkReference()
	{
		var runner = new RecordingRunner();
		var csproj = Path.Combine(
			_root, "Demo.Orders", "Demo.Orders.csproj");
		_fs.WriteFile(
			csproj,
			"<Project>\n" +
			"  <FrameworkReference Include=\"Microsoft.AspNetCore.App\" />\n" +
			"</Project>\n",
			overwrite: false);
		var steps = new DotnetModuleSteps(runner, _console, _fs);

		await steps.RunAsync(
			_root, "Demo", "Demo.Api", "Demo.SharedKernel", "Demo.Orders");

		Assert.Equal(1, _fs.ReadFile(csproj)
			.Split("FrameworkReference").Length - 1);
	}

	[Fact]
	public async Task MigrationFailsFastWhenDotnetEfMissing()
	{
		var runner = new RecordingRunner(failOnCommand: 1, exitCode: 1);
		var steps = new EfMigrationSteps(runner, _console);

		var exit = await steps.TryCreateInitialMigrationAsync(
			_root, "Demo", "Demo.Orders");

		Assert.Equal(1, exit);
		Assert.Single(runner.Commands);
		Assert.Equal("ef --version", runner.Commands[0]);
		Assert.Contains(_console.Errors, m => m.Contains("dotnet-ef"));
	}

	[Fact]
	public async Task MigrationAddsDesignPackageThenCreates()
	{
		var runner = new RecordingRunner();
		var steps = new EfMigrationSteps(runner, _console);

		var exit = await steps.TryCreateInitialMigrationAsync(
			_root, "Demo", "Demo.Orders");

		Assert.Equal(0, exit);
		Assert.Equal(3, runner.Commands.Count);
		Assert.Contains(
			"Demo.Api.csproj package Microsoft.EntityFrameworkCore.Design",
			runner.Commands[1]);
		Assert.Contains(
			"ef migrations add InitialCreate", runner.Commands[2]);
		Assert.Contains(
			"--startup-project Demo.Api", runner.Commands[2]);
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
}

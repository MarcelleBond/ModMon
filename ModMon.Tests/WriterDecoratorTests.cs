using ModMon.Cli;

namespace ModMon.Tests;

public sealed class WriterDecoratorTests
{
	private readonly FakeFileSystemWriter _inner = new();
	private readonly string _path = Path.Combine(
		Path.GetTempPath(), "tracked.txt");

	[Fact]
	public void TrackingSeparatesCreatesFromUpdates()
	{
		var writer = new TrackingFileSystemWriter(_inner);

		writer.WriteFile(_path, "v1", overwrite: true);
		writer.WriteFile(_path, "v2", overwrite: true);

		Assert.Equal(new[] { _path }, writer.Created);
		Assert.Equal(new[] { _path }, writer.Updated);
		Assert.Equal("v2", _inner.ReadFile(_path));
	}

	[Fact]
	public void TrackingSkipsNoOverwriteWrite()
	{
		var writer = new TrackingFileSystemWriter(_inner);
		writer.WriteFile(_path, "v1", overwrite: true);
		writer.Created.Clear();
		writer.Updated.Clear();

		writer.WriteFile(_path, "v2", overwrite: false);

		Assert.Empty(writer.Created);
		Assert.Empty(writer.Updated);
		Assert.Equal("v1", _inner.ReadFile(_path));
	}

	[Fact]
	public void LoggingWriterLogsEachWriteAndDelegates()
	{
		var logger = new RecordingLogger();
		var writer = new LoggingFileSystemWriter(_inner, logger);

		writer.WriteFile(_path, "x", overwrite: true);
		writer.EnsureDirectory("somedir");
		writer.DeleteFile(_path);

		Assert.Equal(3, logger.Debugs.Count);
		Assert.Contains(logger.Debugs, m => m.Contains("write file"));
		Assert.Contains(logger.Debugs, m => m.Contains("ensure directory"));
		Assert.Contains(logger.Debugs, m => m.Contains("delete file"));
		Assert.False(_inner.FileExists(_path));
	}

	[Fact]
	public async Task LoggingRunnerLogsCommandAndExitCode()
	{
		var logger = new RecordingLogger();
		var inner = new StubDotnetRunner(42);
		var runner = new LoggingDotnetCliRunner(inner, logger);

		var exit = await runner.RunAsync("build", @"C:\x");

		Assert.Equal(42, exit);
		Assert.Equal("build", inner.LastArguments);
		Assert.Contains(logger.Infos, m =>
			m.Contains("dotnet build") && m.Contains(@"C:\x"));
		Assert.Contains(logger.Debugs, m => m.Contains("42"));
	}

	[Fact]
	public void LoggingServicesWrapOnlyForRealLogger()
	{
		var fs = new FakeFileSystemWriter();
		var runner = new StubDotnetRunner(0);

		Assert.Same(fs, LoggingServices.Wrap(fs, new NullLogger()));
		Assert.Same(
			runner, LoggingServices.Wrap(runner, new NullLogger()));
		Assert.IsType<LoggingFileSystemWriter>(
			LoggingServices.Wrap(fs, new RecordingLogger()));
		Assert.IsType<LoggingDotnetCliRunner>(
			LoggingServices.Wrap(runner, new RecordingLogger()));
	}

	[Fact]
	public void CompositeLoggerFansOutToEverySink()
	{
		var a = new RecordingLogger();
		var b = new RecordingLogger();
		var composite = new CompositeLogger(a, b);

		composite.LogInfo("i");
		composite.LogDebug("d");
		composite.LogWarning("w");
		composite.LogError("e");

		foreach (var sink in new[] { a, b })
		{
			Assert.Equal(new[] { "i" }, sink.Infos);
			Assert.Equal(new[] { "d" }, sink.Debugs);
			Assert.Equal(new[] { "w" }, sink.Warnings);
			Assert.Equal(new[] { "e" }, sink.Errors);
		}
	}

	[Fact]
	public void FileLoggerCreatesParentDirsAndAppends()
	{
		var dir = Path.Combine(
			Path.GetTempPath(), $"modmon-log-{Guid.NewGuid():N}");
		var file = Path.Combine(dir, "deep", "modmon.log");
		try
		{
			var logger = new FileLogger(file);
			logger.LogInfo("hello");
			logger.LogError("oops");

			var lines = File.ReadAllLines(file);
			Assert.Equal(2, lines.Length);
			Assert.Contains("[INFO] hello", lines[0]);
			Assert.Contains("[ERROR] oops", lines[1]);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	[Fact]
	public void ConsoleLoggerHonoursVerboseFlag()
	{
		var quiet = new RecordingConsole();
		var loud = new RecordingConsole();

		new ConsoleLogger(quiet, verbose: false).LogInfo("msg");
		new ConsoleLogger(loud, verbose: true).LogInfo("msg");

		Assert.Empty(quiet.Lines);
		Assert.Single(loud.Lines);
	}

	private sealed class StubDotnetRunner : IDotnetCliRunner
	{
		private readonly int _exitCode;
		public string? LastArguments { get; private set; }

		public StubDotnetRunner(int exitCode)
		{
			_exitCode = exitCode;
		}

		public Task<int> RunAsync(
			string arguments,
			string workingDirectory)
		{
			LastArguments = arguments;
			return Task.FromResult(_exitCode);
		}
	}
}

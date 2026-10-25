using ModMon.Cli;

namespace ModMon.Tests;

public sealed class DryRunTests
{
	[Fact]
	public void TrackerRecordsOnlyWhenDryRun()
	{
		var inactive = new DryRunTracker(false);
		var active = new DryRunTracker(true);
		var details = new Dictionary<string, object> { ["k"] = "v" };

		inactive.RecordOperation("op", details);
		active.RecordOperation("op", details);

		Assert.Empty(inactive.GetOperations());
		var operation = Assert.Single(active.GetOperations());
		Assert.Equal("op", operation.Type);
		Assert.Equal("v", operation.Details["k"]);
	}

	[Fact]
	public void WriterRecordsCreateWithoutTouchingDisk()
	{
		var inner = new FakeFileSystemWriter();
		var tracker = new DryRunTracker(true);
		var writer = new DryRunFileSystemWriter(inner, tracker);
		var path = Path.Combine(Path.GetTempPath(), "never.txt");

		writer.WriteFile(path, "content", overwrite: true);

		Assert.False(inner.FileExists(path));
		var operation = Assert.Single(tracker.GetOperations());
		Assert.Equal("create-file", operation.Type);
		Assert.Equal(path, operation.Details["path"]);
	}

	[Fact]
	public void WriterRecordsUpdateForExistingFile()
	{
		var inner = new FakeFileSystemWriter();
		var path = Path.Combine(Path.GetTempPath(), "exists.txt");
		inner.WriteFile(path, "old", overwrite: false);
		var tracker = new DryRunTracker(true);
		var writer = new DryRunFileSystemWriter(inner, tracker);

		writer.WriteFile(path, "new", overwrite: true);

		Assert.Equal("old", inner.ReadFile(path));
		Assert.Equal(
			"update-file",
			Assert.Single(tracker.GetOperations()).Type);
	}

	[Fact]
	public void WriterSkipsNoOverwriteLikeRealWriter()
	{
		var inner = new FakeFileSystemWriter();
		var path = Path.Combine(Path.GetTempPath(), "keep.txt");
		inner.WriteFile(path, "old", overwrite: false);
		var tracker = new DryRunTracker(true);
		var writer = new DryRunFileSystemWriter(inner, tracker);

		writer.WriteFile(path, "new", overwrite: false);

		Assert.Empty(tracker.GetOperations());
	}

	[Fact]
	public void WriterRecordsDeletesOnlyForExistingPaths()
	{
		var inner = new FakeFileSystemWriter();
		var file = Path.Combine(Path.GetTempPath(), "del.txt");
		var dir = Path.Combine(Path.GetTempPath(), "deldir");
		inner.WriteFile(file, "x", overwrite: false);
		inner.EnsureDirectory(dir);
		var tracker = new DryRunTracker(true);
		var writer = new DryRunFileSystemWriter(inner, tracker);

		writer.DeleteFile(file);
		writer.DeleteFile(file + ".missing");
		writer.DeleteDirectory(dir);
		writer.DeleteDirectory(dir + "_missing");

		var types = tracker.GetOperations().Select(o => o.Type).ToList();
		Assert.Equal(new[] { "delete-file", "delete-directory" }, types);
	}

	[Fact]
	public void WriterRecordsEnsureDirectoryOnlyWhenMissing()
	{
		var inner = new FakeFileSystemWriter();
		var existing = Path.Combine(Path.GetTempPath(), "dir-exists");
		var missing = Path.Combine(Path.GetTempPath(), "dir-missing");
		inner.EnsureDirectory(existing);
		var tracker = new DryRunTracker(true);
		var writer = new DryRunFileSystemWriter(inner, tracker);

		writer.EnsureDirectory(existing);
		writer.EnsureDirectory(missing);

		var operation = Assert.Single(tracker.GetOperations());
		Assert.Equal("create-directory", operation.Type);
		Assert.Equal(missing, operation.Details["path"]);
		Assert.False(inner.DirectoryExists(missing));
	}

	[Fact]
	public async Task RunnerRecordsCommandAndNeverExecutes()
	{
		var tracker = new DryRunTracker(true);
		var runner = new DryRunDotnetCliRunner(tracker);

		var exit = await runner.RunAsync("new sln -n X", @"C:\work");

		Assert.Equal(0, exit);
		var operation = Assert.Single(tracker.GetOperations());
		Assert.Equal("dotnet-cli", operation.Type);
		Assert.Equal("dotnet new sln -n X", operation.Details["command"]);
		Assert.Equal(@"C:\work", operation.Details["workingDirectory"]);
	}

	[Fact]
	public void WrapDecoratesOnlyWhenDryRun()
	{
		var fs = new FakeFileSystemWriter();
		var dry = new DryRunTracker(true);
		var wet = new DryRunTracker(false);

		Assert.IsType<DryRunFileSystemWriter>(
			DryRunServices.Wrap(fs, dry));
		Assert.Same(fs, DryRunServices.Wrap(fs, wet));
	}

	[Fact]
	public void WrapRunnerDecoratesOnlyWhenDryRun()
	{
		var inner = new DotnetCliRunner(new SystemConsole());
		var dry = new DryRunTracker(true);
		var wet = new DryRunTracker(false);

		Assert.IsType<DryRunDotnetCliRunner>(
			DryRunServices.Wrap(inner, dry));
		Assert.Same(inner, DryRunServices.Wrap(inner, wet));
	}

	[Fact]
	public void NullTrackerIsNeverDryRun()
	{
		var tracker = new NullDryRunTracker();

		Assert.False(tracker.IsDryRun);
		tracker.RecordOperation("op", new Dictionary<string, object>());
		Assert.Empty(tracker.GetOperations());
	}
}

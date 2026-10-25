using ModMon.Cli;

namespace ModMon.Tests;

public sealed class OptionsParseTests
{
	[Fact]
	public void InitParsesAllOptions()
	{
		var options = InitOptions.Parse(
			["--name", "Demo", "--output", "out", "--dry-run"]);

		Assert.True(options.IsValid);
		Assert.Equal("Demo", options.ProjectName);
		Assert.Equal("out", options.OutputPath);
		Assert.True(options.DryRun);
	}

	[Fact]
	public void InitMissingNameIsInvalid()
	{
		var options = InitOptions.Parse(["--dry-run"]);

		Assert.False(options.IsValid);
		Assert.Contains("--name", options.ErrorMessage);
	}

	[Fact]
	public void InitTrailingNameFlagCountsAsMissing()
	{
		var options = InitOptions.Parse(["--name"]);

		Assert.False(options.IsValid);
	}

	[Fact]
	public void InitRejectsInvalidName()
	{
		var options = InitOptions.Parse(["--name", "My App"]);

		Assert.False(options.IsValid);
		Assert.Contains("My App", options.ErrorMessage);
	}

	[Fact]
	public void InitTrimsNameWhitespace()
	{
		var options = InitOptions.Parse(["--name", "  Demo  "]);

		Assert.True(options.IsValid);
		Assert.Equal("Demo", options.ProjectName);
	}

	[Fact]
	public void ModuleParsesAllOptions()
	{
		var options = ModuleAddOptions.Parse(
			["--name", "Orders", "--output", "x", "--dry-run"]);

		Assert.True(options.IsValid);
		Assert.Equal("Orders", options.ModuleName);
		Assert.True(options.DryRun);
	}

	[Fact]
	public void ModuleMissingNameIsInvalid()
	{
		var options = ModuleAddOptions.Parse([]);

		Assert.False(options.IsValid);
		Assert.Contains("--name", options.ErrorMessage);
	}

	[Fact]
	public void ModuleRejectsInvalidName()
	{
		var options = ModuleAddOptions.Parse(["--name", "No-Go"]);

		Assert.False(options.IsValid);
	}

	[Fact]
	public void ModuleIsValidWithoutOptionalFlags()
	{
		var options = ModuleAddOptions.Parse(["--name", "Orders"]);

		Assert.True(options.IsValid);
		Assert.False(options.DryRun);
		Assert.Equal(string.Empty, options.OutputPath);
	}
}

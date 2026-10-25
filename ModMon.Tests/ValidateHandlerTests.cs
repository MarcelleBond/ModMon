using ModMon.Cli;

namespace ModMon.Tests;

public sealed class ValidateHandlerTests
{
	[Fact]
	public async Task ValidSolutionPassesAllChecks()
	{
		using var fixture = await CreateFixtureAsync();
		var handler = CreateHandler(out var output);

		var exit = await handler.HandleAsync(fixture.Root);

		Assert.Equal((int)ExitCode.Success, exit);
		var result = Assert.IsType<ValidateResult>(output.Result);
		Assert.True(result.Success);
		Assert.True(result.Valid);
		Assert.All(result.Checks!, check => Assert.True(check.Passed));
	}

	[Fact]
	public async Task MissingDockerfileFailsCheck()
	{
		using var fixture = await CreateFixtureAsync(skipDockerfile: true);
		var handler = CreateHandler(out var output);

		var exit = await handler.HandleAsync(fixture.Root);

		Assert.Equal((int)ExitCode.ValidationFailed, exit);
		var result = Assert.IsType<ValidateResult>(output.Result);
		Assert.False(result.Success);
		Assert.False(result.Valid);
		Assert.Contains(result.Checks!, check =>
			check.RuleId == ValidationRuleIds.Dockerfile &&
			!check.Passed);
	}

	[Fact]
	public async Task MissingDiAggregatorFailsCheck()
	{
		using var fixture = await CreateFixtureAsync(skipDi: true);
		var handler = CreateHandler(out var output);

		var exit = await handler.HandleAsync(fixture.Root);

		Assert.Equal((int)ExitCode.ValidationFailed, exit);
		var result = Assert.IsType<ValidateResult>(output.Result);
		Assert.Contains(result.Checks!, check =>
			check.RuleId == ValidationRuleIds.DiAggregator &&
			!check.Passed);
	}

	[Fact]
	public async Task BoundaryViolationFailsValidation()
	{
		using var fixture = await CreateFixtureAsync();
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.Api");
		var handler = CreateHandler(out var output);

		var exit = await handler.HandleAsync(fixture.Root);

		Assert.Equal((int)ExitCode.ValidationFailed, exit);
		var result = Assert.IsType<ValidateResult>(output.Result);
		Assert.False(result.Valid);
		Assert.Contains(result.Checks!, check =>
			check.RuleId == ValidationRuleIds.ProjectBoundary &&
			!check.Passed);
	}

	[Fact]
	public async Task MissingSolutionReturnsNotFound()
	{
		var dir = Path.Combine(
			Path.GetTempPath(), $"modmon-noval-{Guid.NewGuid():N}");
		Directory.CreateDirectory(dir);
		try
		{
			var handler = CreateHandler(out var output);

			var exit = await handler.HandleAsync(dir);

			Assert.Equal((int)ExitCode.ResourceNotFound, exit);
			var result = Assert.IsType<ValidateResult>(output.Result);
			Assert.False(result.Success);
			Assert.Contains(result.Checks!, check =>
				check.RuleId == ValidationRuleIds.SolutionDiscovery &&
				!check.Passed);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}

	private static ValidateCommandHandler CreateHandler(
		out RecordingOutputWriter output)
	{
		output = new RecordingOutputWriter();
		return new ValidateCommandHandler(
			new PhysicalFileSystemWriter(), output);
	}

	private static async Task<ProjectFixture> CreateFixtureAsync(
		bool skipDi = false,
		bool skipDockerfile = false)
	{
		var fixture = new ProjectFixture();
		await fixture.AddProjectAsync("Demo.Api");
		await fixture.AddProjectAsync("Demo.SharedKernel");
		await fixture.AddProjectAsync("Demo.Orders");
		await fixture.SetReferencesAsync(
			"Demo.Api", "Demo.Orders", "Demo.SharedKernel");
		await fixture.SetReferencesAsync("Demo.Orders", "Demo.SharedKernel");
		await fixture.WriteSolutionAsync();

		if (!skipDi)
		{
			var diDir = Path.Combine(
				fixture.Root, "Demo.Api", "Extensions");
			Directory.CreateDirectory(diDir);
			await File.WriteAllTextAsync(
				Path.Combine(diDir, "DependencyInjection.cs"), "// di");
		}

		if (!skipDockerfile)
		{
			await File.WriteAllTextAsync(
				Path.Combine(fixture.Root, "Dockerfile"), "FROM sdk");
		}

		return fixture;
	}
}

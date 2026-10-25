using System.Text.Json;
using ModMon.Cli;

namespace ModMon.Tests;

public sealed class OutputWriterTests
{
	[Fact]
	public void JsonErrorWithResultDetailsStaysFlat()
	{
		var writer = new JsonOutputWriter();
		var result = new InitResult(
			Success: false,
			Message: "Solution already exists",
			ExitCode: ExitCode.ResourceAlreadyExists,
			Timestamp: DateTime.UtcNow,
			Errors: new List<string> { "already there" });

		writer.WriteError("Solution already exists", result);
		var json = CaptureConsoleOut(() => writer.Flush());
		var doc = JsonDocument.Parse(json);

		Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
		Assert.Equal(
			"init", doc.RootElement.GetProperty("command").GetString());
		Assert.False(doc.RootElement.TryGetProperty(
			"details", out _));
	}

	[Fact]
	public void JsonErrorWithoutDetailsWrapsMessage()
	{
		var writer = new JsonOutputWriter();

		writer.WriteError("bad things");
		var json = CaptureConsoleOut(() => writer.Flush());
		var doc = JsonDocument.Parse(json);

		Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
		Assert.Equal(
			"bad things",
			doc.RootElement.GetProperty("message").GetString());
	}

	[Fact]
	public void JsonSuccessSerializesCamelCase()
	{
		var writer = new JsonOutputWriter();

		writer.WriteSuccess(new ModuleListResult(
			Success: true,
			Message: "ok",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: "Demo",
			Count: 2));
		var json = CaptureConsoleOut(() => writer.Flush());
		var doc = JsonDocument.Parse(json);

		Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
		Assert.Equal(
			"Demo",
			doc.RootElement.GetProperty("projectName").GetString());
		Assert.Equal(
			"module list",
			doc.RootElement.GetProperty("command").GetString());
	}

	[Fact]
	public void JsonFlushWithoutResultWritesNothing()
	{
		var writer = new JsonOutputWriter();

		var json = CaptureConsoleOut(() => writer.Flush());

		Assert.Equal(string.Empty, json);
	}

	[Fact]
	public void HumanInitRendersSectionsNotJson()
	{
		var console = new RecordingConsole();
		var writer = new HumanOutputWriter(console);
		var result = new InitResult(
			Success: true,
			Message: "done",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: "Demo",
			CreatedFiles: new List<string> { "a.cs" },
			UpdatedFiles: new List<string> { "b.json" },
			SkippedFiles: new List<string> { "AGENTS.md" });

		writer.WriteSuccess(result);

		Assert.Contains(
			console.Lines, l => l.Contains("initialized successfully"));
		Assert.Contains(console.Lines, l => l.Contains("a.cs"));
		Assert.Contains(console.Lines, l => l.Contains("b.json"));
		Assert.Contains(console.Lines, l => l.Contains("AGENTS.md"));
		Assert.DoesNotContain(console.Lines, l => l.StartsWith("{"));
	}

	[Fact]
	public void HumanValidateRendersPassFailTags()
	{
		var console = new RecordingConsole();
		var writer = new HumanOutputWriter(console);
		var result = new ValidateResult(
			Success: false,
			Message: "invalid",
			ExitCode: ExitCode.ValidationFailed,
			Timestamp: DateTime.UtcNow,
			Valid: false,
			ProjectName: "Demo",
			Checks: new List<ValidationCheck>
			{
				new("Solution file exists", true),
				new("Boundary", false, "violation!")
			});

		writer.WriteSuccess(result);

		Assert.Contains(console.Lines, l => l.Contains("[PASS]"));
		Assert.Contains(console.Lines, l =>
			l.Contains("[FAIL]") && l.Contains("violation!"));
		Assert.Contains(console.Lines, l => l.Contains("INVALID"));
	}

	[Fact]
	public void HumanModuleAddShowsMigrationStateAndWarnings()
	{
		var console = new RecordingConsole();
		var writer = new HumanOutputWriter(console);
		var result = new ModuleAddResult(
			Success: true,
			Message: "ok",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ModuleName: "Orders",
			MigrationCreated: false,
			DryRun: true,
			Warnings: new List<string> { "no ef tool" });

		writer.WriteSuccess(result);

		Assert.Contains(console.Lines, l =>
			l.Contains("Dry run") && l.Contains("no changes"));
		Assert.Contains(console.Lines, l =>
			l.Contains("EF migration") && l.Contains("would create"));
		Assert.Contains(console.Lines, l => l.Contains("no ef tool"));
	}

	[Fact]
	public void HumanModuleListRendersModuleRows()
	{
		var console = new RecordingConsole();
		var writer = new HumanOutputWriter(console);
		var result = new ModuleListResult(
			Success: true,
			Message: "ok",
			ExitCode: ExitCode.Success,
			Timestamp: DateTime.UtcNow,
			ProjectName: "Demo",
			Count: 1,
			Modules: new List<ModuleInfo>
			{
				new("Orders", @"C:\x\Demo.Orders", true, false)
			});

		writer.WriteSuccess(result);

		Assert.Contains(console.Lines, l =>
			l.Contains("Orders") && l.Contains("DbContext: yes"));
	}

	[Fact]
	public void HumanErrorWritesResultErrorsToConsole()
	{
		var console = new RecordingConsole();
		var writer = new HumanOutputWriter(console);
		var result = new InitResult(
			Success: false,
			Message: "top",
			ExitCode: ExitCode.GeneralFailure,
			Timestamp: DateTime.UtcNow,
			Errors: new List<string> { "top", "extra detail" });

		writer.WriteError("top", result);

		Assert.Contains("top", console.Errors);
		Assert.Contains("  - extra detail", console.Errors);
		Assert.DoesNotContain("  - top", console.Errors);
	}

	[Fact]
	public void StderrConsoleRedirectsLinesOnly()
	{
		var inner = new RecordingConsole();
		var console = new StderrConsole(inner);
		var stderr = CaptureConsoleErr(() =>
			console.WriteLine("progress"));

		Assert.Contains("progress", stderr);
		Assert.Empty(inner.Lines);

		console.WriteError("bad");
		Assert.Contains("bad", inner.Errors);
	}

	private static string CaptureConsoleOut(Action action)
	{
		var original = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);
		try
		{
			action();
			return writer.ToString();
		}
		finally
		{
			Console.SetOut(original);
		}
	}

	private static string CaptureConsoleErr(Action action)
	{
		var original = Console.Error;
		using var writer = new StringWriter();
		Console.SetError(writer);
		try
		{
			action();
			return writer.ToString();
		}
		finally
		{
			Console.SetError(original);
		}
	}
}

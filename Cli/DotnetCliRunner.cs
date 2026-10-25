using System.Diagnostics;

namespace ModMon.Cli;

internal sealed class DotnetCliRunner : IDotnetCliRunner
{
	private readonly IConsole _console;

	public DotnetCliRunner(IConsole console)
	{
		_console = console;
	}

	public async Task<int> RunAsync(string arguments, string workingDirectory)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = arguments,
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = startInfo };
		try
		{
			return await RunProcessAsync(process);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_console.WriteError(
				$"Failed to run 'dotnet {arguments}': {ex.Message}");
			return 1;
		}
	}

	private async Task<int> RunProcessAsync(Process process)
	{
		process.OutputDataReceived += (_, e) =>
		{
			if (!string.IsNullOrWhiteSpace(e.Data))
			{
				_console.WriteLine(e.Data);
			}
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (!string.IsNullOrWhiteSpace(e.Data))
			{
				_console.WriteError(e.Data);
			}
		};

		if (!process.Start())
		{
			_console.WriteError("Failed to start dotnet process.");
			return 1;
		}

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		await process.WaitForExitAsync();
		return process.ExitCode;
	}
}

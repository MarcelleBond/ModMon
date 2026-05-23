namespace ModMon.Cli;

public record CommandResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	string Command,
	DateTime Timestamp,
	Dictionary<string, object>? Data = null,
	List<string>? Errors = null,
	List<string>? Warnings = null);

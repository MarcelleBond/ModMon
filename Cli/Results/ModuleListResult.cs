namespace ModMon.Cli;

public record ModuleListResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	DateTime Timestamp,
	string? ProjectName = null,
	List<ModuleInfo>? Modules = null,
	int Count = 0,
	Dictionary<string, object>? Data = null,
	List<string>? Errors = null,
	List<string>? Warnings = null)
	: CommandResult(
		Success,
		Message,
		ExitCode,
		"module list",
		Timestamp,
		Data,
		Errors,
		Warnings);

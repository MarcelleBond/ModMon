namespace ModMon.Cli;

public record ModuleAddResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	DateTime Timestamp,
	string? ModuleName = null,
	string? ModulePath = null,
	bool MigrationCreated = false,
	List<string>? UpdatedFiles = null,
	List<string>? CreatedFiles = null,
	bool DryRun = false,
	List<DryRunOperation>? Operations = null,
	Dictionary<string, object>? Data = null,
	List<string>? Errors = null,
	List<string>? Warnings = null)
	: CommandResult(
		Success,
		Message,
		ExitCode,
		"module add",
		Timestamp,
		Data,
		Errors,
		Warnings);

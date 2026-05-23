namespace ModMon.Cli;

public record InitResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	DateTime Timestamp,
	string? ProjectName = null,
	string? SolutionPath = null,
	string? RepoRoot = null,
	List<string>? CreatedProjects = null,
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
		"init",
		Timestamp,
		Data,
		Errors,
		Warnings);

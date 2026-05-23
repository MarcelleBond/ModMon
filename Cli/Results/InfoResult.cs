namespace ModMon.Cli;

public record InfoResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	DateTime Timestamp,
	string? ProjectName = null,
	string? SolutionPath = null,
	string? RepoRoot = null,
	List<ModuleInfo>? Modules = null,
	List<string>? Projects = null,
	Dictionary<string, object>? Data = null,
	List<string>? Errors = null,
	List<string>? Warnings = null)
	: CommandResult(
		Success,
		Message,
		ExitCode,
		"info",
		Timestamp,
		Data,
		Errors,
		Warnings);

public record ModuleInfo(
	string Name,
	string Path,
	bool HasDbContext,
	bool HasMigrations);

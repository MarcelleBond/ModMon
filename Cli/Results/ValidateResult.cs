namespace ModMon.Cli;

public record ValidateResult(
	bool Success,
	string Message,
	ExitCode ExitCode,
	DateTime Timestamp,
	bool Valid,
	string? ProjectName = null,
	List<ValidationCheck>? Checks = null,
	Dictionary<string, object>? Data = null,
	List<string>? Errors = null,
	List<string>? Warnings = null)
	: CommandResult(
		Success,
		Message,
		ExitCode,
		"validate",
		Timestamp,
		Data,
		Errors,
		Warnings);

public record ValidationCheck(
	string Name,
	bool Passed,
	string? Message = null);

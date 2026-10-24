namespace ModMon.Cli;

/// <summary>
/// Represents one validation rule outcome and its optional diagnostic.
/// </summary>
/// <param name="Name">Human-readable name of the validation check.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="Message">Failure detail. Null when none is needed.</param>
/// <param name="RuleId">Stable automation identifier, when available.</param>
/// <param name="ProjectPath">Project file associated with the finding.</param>
public record ValidationCheck(
	string Name,
	bool Passed,
	string? Message = null,
	string? RuleId = null,
	string? ProjectPath = null);
